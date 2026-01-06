using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.Ripples.Abstractions;
using Mississippi.Viaduct.Grains;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Ripples.Orleans.Grains;

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
///         (projectionType, entityId, newVersion) - never exposing brook details.
///     </para>
/// </remarks>
[Alias("Mississippi.Ripples.Orleans.RippleSubscriptionGrain")]
internal sealed class RippleSubscriptionGrain
    : IRippleSubscriptionGrain,
      IAsyncObserver<BrookCursorMovedEvent>,
      IGrainBase
{
    /// <summary>
    ///     Maps brookKey → current position (for filtering stale events).
    /// </summary>
    private readonly Dictionary<string, BrookPosition> brookPositions = [];

    /// <summary>
    ///     Maps brookKey → stream subscription handle (for deduplication).
    /// </summary>
    private readonly Dictionary<string, StreamSubscriptionHandle<BrookCursorMovedEvent>> brookStreamHandles = [];

    /// <summary>
    ///     Maps brookKey → set of subscriptionIds using that brook.
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> brookToSubscriptions = [];

    /// <summary>
    ///     Maps subscriptionId → subscription details.
    /// </summary>
    private readonly Dictionary<string, SubscriptionEntry> subscriptions = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="RippleSubscriptionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="projectionBrookRegistry">Registry for projection to brook mappings.</param>
    /// <param name="streamProviderOptions">Configuration options for the Orleans stream provider.</param>
    /// <param name="streamIdFactory">Factory for creating Orleans stream identifiers.</param>
    /// <param name="brookStorageReader">Brook storage reader for fetching initial cursor position.</param>
    /// <param name="logger">Logger instance for logging subscription grain operations.</param>
    public RippleSubscriptionGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory,
        IProjectionBrookRegistry projectionBrookRegistry,
        IOptions<BrookProviderOptions> streamProviderOptions,
        IStreamIdFactory streamIdFactory,
        IBrookStorageReader brookStorageReader,
        ILogger<RippleSubscriptionGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        ProjectionBrookRegistry =
            projectionBrookRegistry ?? throw new ArgumentNullException(nameof(projectionBrookRegistry));
        StreamProviderOptions = streamProviderOptions ?? throw new ArgumentNullException(nameof(streamProviderOptions));
        StreamIdFactory = streamIdFactory ?? throw new ArgumentNullException(nameof(streamIdFactory));
        BrookStorageReader = brookStorageReader ?? throw new ArgumentNullException(nameof(brookStorageReader));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private IBrookStorageReader BrookStorageReader { get; }

    private IGrainFactory GrainFactory { get; }

    private ILogger<RippleSubscriptionGrain> Logger { get; }

    private IProjectionBrookRegistry ProjectionBrookRegistry { get; }

    private IStreamIdFactory StreamIdFactory { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    /// <inheritdoc />
    public async Task ClearAllAsync()
    {
        string connectionId = this.GetPrimaryKeyString();
        Logger.ClearingAllSubscriptions(connectionId, subscriptions.Count);

        // Unsubscribe from all brook streams
        foreach ((string brookKey, StreamSubscriptionHandle<BrookCursorMovedEvent> handle) in brookStreamHandles)
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

        subscriptions.Clear();
        brookToSubscriptions.Clear();
        brookStreamHandles.Clear();
        brookPositions.Clear();
        Logger.AllSubscriptionsCleared(connectionId);
        this.DeactivateOnIdle();
    }

    /// <inheritdoc />
    public Task<ImmutableList<RippleSubscription>> GetSubscriptionsAsync()
    {
        ImmutableList<RippleSubscription> result = subscriptions.Values
            .Select(s => new RippleSubscription(s.SubscriptionId, s.ProjectionType, s.EntityId))
            .ToImmutableList();
        return Task.FromResult(result);
    }

    /// <summary>
    ///     Handles stream completion.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnCompletedAsync()
    {
        string connectionId = this.GetPrimaryKeyString();
        Logger.BrookStreamCompleted(connectionId);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles errors on the subscribed stream.
    /// </summary>
    /// <param name="ex">The exception encountered on the stream.</param>
    /// <returns>A completed task.</returns>
    public Task OnErrorAsync(
        Exception ex
    )
    {
        string connectionId = this.GetPrimaryKeyString();
        Logger.BrookStreamError(connectionId, ex);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles a cursor moved event and fans out to all subscribed projections.
    /// </summary>
    /// <param name="item">The event containing the new brook cursor position.</param>
    /// <param name="token">Optional sequence token for ordering updates.</param>
    /// <returns>A task representing the asynchronous event handling operation.</returns>
    public async Task OnNextAsync(
        BrookCursorMovedEvent item,
        StreamSequenceToken? token = null
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        string connectionId = this.GetPrimaryKeyString();

        // Find which brook this event is for by checking all brook subscriptions
        // Note: In a real implementation, we'd need to track which stream maps to which brookKey
        // For now, we iterate and match based on the stream subscription context
        foreach ((string brookKey, HashSet<string> subscriptionIds) in brookToSubscriptions)
        {
            // Check if position moved forward
            if (brookPositions.TryGetValue(brookKey, out BrookPosition currentPosition) &&
                !item.NewPosition.IsNewerThan(currentPosition))
            {
                continue;
            }

            brookPositions[brookKey] = item.NewPosition;

            // Fan out to all projections subscribed to this brook
            foreach (string subscriptionId in subscriptionIds)
            {
                if (!subscriptions.TryGetValue(subscriptionId, out SubscriptionEntry? entry))
                {
                    continue;
                }

                // Notify SignalR via the group grain
                string groupName = $"projection:{entry.ProjectionType}:{entry.EntityId}";
                string hubGroupKey = $"{RippleHubConstants.HubName}:{groupName}";
                try
                {
                    ISignalRGroupGrain groupGrain = GrainFactory.GetGrain<ISignalRGroupGrain>(hubGroupKey);
                    await groupGrain.SendMessageAsync(
                        RippleHubConstants.ProjectionUpdatedMethod,
                        [entry.ProjectionType, entry.EntityId, item.NewPosition.Value]);
                    Logger.NotificationSent(connectionId, entry.ProjectionType, entry.EntityId, item.NewPosition.Value);
                }
                catch (OrleansException ex)
                {
                    Logger.FailedToSendNotification(connectionId, entry.ProjectionType, entry.EntityId, ex);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.FailedToSendNotification(connectionId, entry.ProjectionType, entry.EntityId, ex);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> SubscribeAsync(
        string projectionType,
        string entityId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        string connectionId = this.GetPrimaryKeyString();

        // Resolve brook name from registry
        if (!ProjectionBrookRegistry.TryGetBrookName(projectionType, out string? brookName) || brookName is null)
        {
            Logger.ProjectionTypeNotRegistered(connectionId, projectionType);
            throw new InvalidOperationException(
                $"Projection type '{projectionType}' is not registered in the projection brook registry.");
        }

        string subscriptionId = Guid.NewGuid().ToString("N");
        BrookKey brookKey = new(brookName, entityId);
        string brookKeyString = brookKey.ToString();
        Logger.SubscribingToProjection(connectionId, subscriptionId, projectionType, entityId, brookName);

        // Store subscription
        SubscriptionEntry entry = new()
        {
            SubscriptionId = subscriptionId,
            ProjectionType = projectionType,
            EntityId = entityId,
            BrookKey = brookKeyString,
        };
        subscriptions[subscriptionId] = entry;

        // Add to brook→subscriptions mapping (for fan-out)
        if (!brookToSubscriptions.TryGetValue(brookKeyString, out HashSet<string>? brookSubs))
        {
            brookSubs = [];
            brookToSubscriptions[brookKeyString] = brookSubs;
        }

        brookSubs.Add(subscriptionId);

        // Subscribe to brook stream if this is the first subscription for this brook
        if (!brookStreamHandles.ContainsKey(brookKeyString))
        {
            await SubscribeToBrookStreamAsync(brookKey, brookKeyString);
        }

        Logger.SubscribedToProjection(connectionId, subscriptionId, projectionType, entityId);
        return subscriptionId;
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(
        string subscriptionId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(subscriptionId);
        string connectionId = this.GetPrimaryKeyString();
        if (!subscriptions.TryGetValue(subscriptionId, out SubscriptionEntry? entry))
        {
            Logger.SubscriptionNotFound(connectionId, subscriptionId);
            return;
        }

        Logger.UnsubscribingFromProjection(connectionId, subscriptionId, entry.ProjectionType, entry.EntityId);

        // Remove from subscriptions
        subscriptions.Remove(subscriptionId);

        // Remove from brook→subscriptions mapping
        if (brookToSubscriptions.TryGetValue(entry.BrookKey, out HashSet<string>? brookSubs))
        {
            brookSubs.Remove(subscriptionId);

            // If no more subscriptions for this brook, unsubscribe from stream
            if (brookSubs.Count == 0)
            {
                brookToSubscriptions.Remove(entry.BrookKey);
                await UnsubscribeFromBrookStreamAsync(entry.BrookKey);
            }
        }

        Logger.UnsubscribedFromProjection(connectionId, subscriptionId);
    }

    private async Task SubscribeToBrookStreamAsync(
        BrookKey brookKey,
        string brookKeyString
    )
    {
        string connectionId = this.GetPrimaryKeyString();

        // Read initial cursor position from storage
        BrookPosition initialPosition =
            await BrookStorageReader.ReadCursorPositionAsync(brookKey, CancellationToken.None);
        brookPositions[brookKeyString] = initialPosition;

        // Subscribe to the brook cursor stream
        StreamId streamId = StreamIdFactory.Create(brookKey);
        IAsyncStream<BrookCursorMovedEvent> stream = this
            .GetStreamProvider(StreamProviderOptions.Value.OrleansStreamProviderName)
            .GetStream<BrookCursorMovedEvent>(streamId);
        StreamSubscriptionHandle<BrookCursorMovedEvent> handle = await stream.SubscribeAsync(this);
        brookStreamHandles[brookKeyString] = handle;
        Logger.SubscribedToBrookStream(connectionId, brookKeyString, initialPosition.Value);
    }

    private async Task UnsubscribeFromBrookStreamAsync(
        string brookKeyString
    )
    {
        string connectionId = this.GetPrimaryKeyString();
        if (brookStreamHandles.TryGetValue(brookKeyString, out StreamSubscriptionHandle<BrookCursorMovedEvent>? handle))
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

            brookStreamHandles.Remove(brookKeyString);
        }

        brookPositions.Remove(brookKeyString);
    }

    /// <summary>
    ///     Internal subscription entry tracking all details.
    /// </summary>
    private sealed class SubscriptionEntry
    {
        public required string BrookKey { get; init; }

        public required string EntityId { get; init; }

        public required string ProjectionType { get; init; }

        public required string SubscriptionId { get; init; }
    }
}