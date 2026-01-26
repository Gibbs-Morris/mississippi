using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.Inlet.Silo.Abstractions;
using Mississippi.Inlet.Server.Abstractions;
using Mississippi.Inlet.Silo.Diagnostics;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Inlet.Silo.Grains;

/// <summary>
///     Orleans grain implementation managing projection subscriptions for a single SignalR connection.
/// </summary>
/// <remarks>
///     <para>
///         This grain is keyed by the SignalR ConnectionId and maintains all active projection
///         subscriptions for that connection. It handles brook stream subscriptions with
///         deduplication - multiple projections using the same brook and entity ID share
///         a single stream subscription.
///     </para>
///     <para>
///         When a brook cursor moves, the grain fans out notifications to all projection
///         subscriptions sharing that brook, notifying SignalR clients with
///         (path, entityId, newVersion) - never exposing brook details.
///     </para>
/// </remarks>
[Alias("Mississippi.Inlet.Orleans.InletSubscriptionGrain")]
internal sealed class InletSubscriptionGrain
    : IInletSubscriptionGrain,
      IAsyncObserver<BrookCursorMovedEvent>,
      IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSubscriptionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="aqueductGrainFactory">Factory for resolving Aqueduct grains.</param>
    /// <param name="projectionBrookRegistry">Registry for projection path to brook mappings.</param>
    /// <param name="streamProviderOptions">Configuration options for the Orleans stream provider.</param>
    /// <param name="streamIdFactory">Factory for creating Orleans stream identifiers.</param>
    /// <param name="brookStorageReader">Brook storage reader for fetching initial cursor position.</param>
    /// <param name="logger">Logger instance for logging subscription grain operations.</param>
    public InletSubscriptionGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory,
        IAqueductGrainFactory aqueductGrainFactory,
        IProjectionBrookRegistry projectionBrookRegistry,
        IOptions<BrookProviderOptions> streamProviderOptions,
        IStreamIdFactory streamIdFactory,
        IBrookStorageReader brookStorageReader,
        ILogger<InletSubscriptionGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        AqueductGrainFactory = aqueductGrainFactory ?? throw new ArgumentNullException(nameof(aqueductGrainFactory));
        ProjectionBrookRegistry =
            projectionBrookRegistry ?? throw new ArgumentNullException(nameof(projectionBrookRegistry));
        StreamProviderOptions = streamProviderOptions ?? throw new ArgumentNullException(nameof(streamProviderOptions));
        StreamIdFactory = streamIdFactory ?? throw new ArgumentNullException(nameof(streamIdFactory));
        BrookStorageReader = brookStorageReader ?? throw new ArgumentNullException(nameof(brookStorageReader));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IAqueductGrainFactory AqueductGrainFactory { get; }

    private Dictionary<string, BrookPosition> BrookPositions { get; } = [];

    private IBrookStorageReader BrookStorageReader { get; }

    private Dictionary<string, StreamSubscriptionHandle<BrookCursorMovedEvent>> BrookStreamHandles { get; } = [];

    private Dictionary<string, HashSet<string>> BrookToSubscriptions { get; } = [];

    private IGrainFactory GrainFactory { get; }

    private ILogger<InletSubscriptionGrain> Logger { get; }

    private IProjectionBrookRegistry ProjectionBrookRegistry { get; }

    private IStreamIdFactory StreamIdFactory { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    private Dictionary<string, SubscriptionEntry> Subscriptions { get; } = [];

    /// <inheritdoc />
    public async Task ClearAllAsync()
    {
        string connectionId = this.GetPrimaryKeyString();
        Logger.ClearingAllSubscriptions(connectionId, Subscriptions.Count);
        foreach ((string brookKey, StreamSubscriptionHandle<BrookCursorMovedEvent> handle) in BrookStreamHandles)
        {
            try
            {
                await handle.UnsubscribeAsync();
                Logger.UnsubscribedFromBrookStream(connectionId, brookKey);
            }
            catch (OrleansException ex)
            {
                Logger.FailedToUnsubscribeFromBrookStream(connectionId, brookKey, ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.FailedToUnsubscribeFromBrookStream(connectionId, brookKey, ex);
            }
        }

        Subscriptions.Clear();
        BrookToSubscriptions.Clear();
        BrookStreamHandles.Clear();
        BrookPositions.Clear();
        Logger.AllSubscriptionsCleared(connectionId);
        this.DeactivateOnIdle();
    }

    /// <inheritdoc />
    public Task<ImmutableList<InletSubscription>> GetSubscriptionsAsync()
    {
        ImmutableList<InletSubscription> result = Subscriptions.Values
            .Select(s => new InletSubscription(s.SubscriptionId, s.Path, s.EntityId))
            .ToImmutableList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task OnCompletedAsync()
    {
        string connectionId = this.GetPrimaryKeyString();
        Logger.BrookStreamCompleted(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnErrorAsync(
        Exception ex
    )
    {
        string connectionId = this.GetPrimaryKeyString();
        Logger.BrookStreamError(connectionId, ex);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task OnNextAsync(
        BrookCursorMovedEvent item,
        StreamSequenceToken? token = null
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        string connectionId = this.GetPrimaryKeyString();
        string brookKey = item.BrookKey;
        InletMetrics.RecordCursorEventReceived(brookKey);
        Logger.ReceivedCursorMovedEvent(connectionId, brookKey, item.NewPosition.Value);

        // Only process the specific brook that triggered this event
        if (!BrookToSubscriptions.TryGetValue(brookKey, out HashSet<string>? subscriptionIds))
        {
            // No subscriptions for this brook (shouldn't happen, but be defensive)
            Logger.NoBrookSubscriptionsFound(connectionId, brookKey, BrookToSubscriptions.Count);
            return;
        }

        Logger.FoundBrookSubscriptions(connectionId, brookKey, subscriptionIds.Count);

        // Check if this is a newer position than we've seen
        if (BrookPositions.TryGetValue(brookKey, out BrookPosition currentPosition) &&
            !item.NewPosition.IsNewerThan(currentPosition))
        {
            Logger.SkippingOlderPosition(connectionId, brookKey, item.NewPosition.Value, currentPosition.Value);
            return;
        }

        BrookPositions[brookKey] = item.NewPosition;
        foreach (string subscriptionId in subscriptionIds)
        {
            if (!Subscriptions.TryGetValue(subscriptionId, out SubscriptionEntry? entry))
            {
                Logger.SubscriptionEntryNotFound(connectionId, subscriptionId);
                continue;
            }

            Logger.SendingToSignalRClient(connectionId, entry.Path, entry.EntityId, item.NewPosition.Value);
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                ISignalRClientGrain clientGrain = AqueductGrainFactory.GetClientGrain(
                    InletHubConstants.HubName,
                    connectionId);
                await clientGrain.SendMessageAsync(
                    InletHubConstants.ProjectionUpdatedMethod,
                    [entry.Path, entry.EntityId, item.NewPosition.Value]);
                sw.Stop();
                InletMetrics.RecordNotificationSent(entry.Path, sw.Elapsed.TotalMilliseconds);
                Logger.NotificationSent(connectionId, entry.Path, entry.EntityId, item.NewPosition.Value);
            }
            catch (OrleansException ex)
            {
                sw.Stop();
                InletMetrics.RecordNotificationError(entry.Path, ex.GetType().Name);
                Logger.FailedToSendNotification(connectionId, entry.Path, entry.EntityId, ex);
            }
            catch (InvalidOperationException ex)
            {
                sw.Stop();
                InletMetrics.RecordNotificationError(entry.Path, ex.GetType().Name);
                Logger.FailedToSendNotification(connectionId, entry.Path, entry.EntityId, ex);
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> SubscribeAsync(
        string path,
        string entityId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        string connectionId = this.GetPrimaryKeyString();
        if (!ProjectionBrookRegistry.TryGetBrookName(path, out string? brookName) || brookName is null)
        {
            Logger.ProjectionPathNotRegistered(connectionId, path);
            throw new InvalidOperationException(
                $"Projection path '{path}' is not registered in the projection brook registry.");
        }

        string subscriptionId = Guid.NewGuid().ToString("N");
        BrookKey brookKey = new(brookName, entityId);
        string brookKeyString = brookKey.ToString();
        Logger.SubscribingToProjection(connectionId, subscriptionId, path, entityId, brookName);
        SubscriptionEntry entry = new()
        {
            SubscriptionId = subscriptionId,
            Path = path,
            EntityId = entityId,
            BrookKey = brookKeyString,
        };
        Subscriptions[subscriptionId] = entry;
        if (!BrookToSubscriptions.TryGetValue(brookKeyString, out HashSet<string>? brookSubs))
        {
            brookSubs = [];
            BrookToSubscriptions[brookKeyString] = brookSubs;
        }

        brookSubs.Add(subscriptionId);
        if (!BrookStreamHandles.ContainsKey(brookKeyString))
        {
            await SubscribeToBrookStreamAsync(brookKey, brookKeyString);
        }

        InletMetrics.RecordSubscription(path, "subscribe");
        Logger.SubscribedToProjection(connectionId, subscriptionId, path, entityId);
        return subscriptionId;
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(
        string subscriptionId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(subscriptionId);
        string connectionId = this.GetPrimaryKeyString();
        if (!Subscriptions.TryGetValue(subscriptionId, out SubscriptionEntry? entry))
        {
            Logger.SubscriptionNotFound(connectionId, subscriptionId);
            return;
        }

        Logger.UnsubscribingFromProjection(connectionId, subscriptionId, entry.Path, entry.EntityId);
        Subscriptions.Remove(subscriptionId);
        if (BrookToSubscriptions.TryGetValue(entry.BrookKey, out HashSet<string>? brookSubs))
        {
            brookSubs.Remove(subscriptionId);
            if (brookSubs.Count == 0)
            {
                BrookToSubscriptions.Remove(entry.BrookKey);
                await UnsubscribeFromBrookStreamAsync(entry.BrookKey);
            }
        }

        InletMetrics.RecordSubscription(entry.Path, "unsubscribe");
        Logger.UnsubscribedFromProjection(connectionId, subscriptionId);
    }

    private async Task SubscribeToBrookStreamAsync(
        BrookKey brookKey,
        string brookKeyString
    )
    {
        string connectionId = this.GetPrimaryKeyString();
        BrookPosition initialPosition =
            await BrookStorageReader.ReadCursorPositionAsync(brookKey, CancellationToken.None);
        BrookPositions[brookKeyString] = initialPosition;
        StreamId streamId = StreamIdFactory.Create(brookKey);
        IAsyncStream<BrookCursorMovedEvent>? stream = this
            .GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<BrookCursorMovedEvent>(streamId);
        StreamSubscriptionHandle<BrookCursorMovedEvent> handle = await stream.SubscribeAsync(this);
        BrookStreamHandles[brookKeyString] = handle;
        Logger.SubscribedToBrookStream(connectionId, brookKeyString, initialPosition.Value);
    }

    private async Task UnsubscribeFromBrookStreamAsync(
        string brookKeyString
    )
    {
        string connectionId = this.GetPrimaryKeyString();
        if (BrookStreamHandles.TryGetValue(brookKeyString, out StreamSubscriptionHandle<BrookCursorMovedEvent>? handle))
        {
            try
            {
                await handle.UnsubscribeAsync();
                Logger.UnsubscribedFromBrookStream(connectionId, brookKeyString);
            }
            catch (OrleansException ex)
            {
                Logger.FailedToUnsubscribeFromBrookStream(connectionId, brookKeyString, ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.FailedToUnsubscribeFromBrookStream(connectionId, brookKeyString, ex);
            }

            BrookStreamHandles.Remove(brookKeyString);
        }

        BrookPositions.Remove(brookKeyString);
    }

    private sealed class SubscriptionEntry
    {
        public required string BrookKey { get; init; }

        public required string EntityId { get; init; }

        public required string Path { get; init; }

        public required string SubscriptionId { get; init; }
    }
}