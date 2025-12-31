using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Brooks.Reader;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions.Subscriptions;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.EventSourcing.UxProjections.Subscriptions;

/// <summary>
///     Orleans grain implementation that manages projection subscriptions for a single SignalR connection.
/// </summary>
/// <remarks>
///     <para>
///         This grain is keyed by the SignalR ConnectionId and maintains all active projection
///         subscriptions for that connection. When a subscribed projection's cursor moves,
///         the grain publishes a <see cref="UxProjectionChangedEvent" /> to the per-connection
///         output stream.
///     </para>
///     <para>
///         State is maintained in-memory for the lifetime of the connection. When the connection
///         disconnects, the grain is deactivated and state is lost. This is intentional as
///         subscription state is ephemeral and tied to the active connection.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.UxProjectionSubscriptionGrain")]
internal sealed class UxProjectionSubscriptionGrain : IUxProjectionSubscriptionGrain, IGrainBase
{
    private readonly Dictionary<string, ActiveSubscription> subscriptions = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionSubscriptionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="streamProviderOptions">Configuration options for the Orleans stream provider.</param>
    /// <param name="streamIdFactory">Factory for creating Orleans stream identifiers.</param>
    /// <param name="logger">Logger instance for logging subscription grain operations.</param>
    public UxProjectionSubscriptionGrain(
        IGrainContext grainContext,
        IOptions<BrookProviderOptions> streamProviderOptions,
        IStreamIdFactory streamIdFactory,
        ILogger<UxProjectionSubscriptionGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        StreamProviderOptions = streamProviderOptions ?? throw new ArgumentNullException(nameof(streamProviderOptions));
        StreamIdFactory = streamIdFactory ?? throw new ArgumentNullException(nameof(streamIdFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<UxProjectionSubscriptionGrain> Logger { get; }

    private IStreamIdFactory StreamIdFactory { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    /// <inheritdoc />
    public async Task ClearAllAsync()
    {
        string connectionId = this.GetPrimaryKeyString();
        Logger.ClearingAllSubscriptions(connectionId, subscriptions.Count);

        foreach (ActiveSubscription subscription in subscriptions.Values
            .Where(s => s.StreamHandle is not null))
        {
            await subscription.StreamHandle!.UnsubscribeAsync();
        }

        subscriptions.Clear();

        Logger.AllSubscriptionsCleared(connectionId);

        this.DeactivateOnIdle();
    }

    /// <inheritdoc />
    public Task<ImmutableList<UxProjectionSubscriptionRequest>> GetSubscriptionsAsync()
    {
        ImmutableList<UxProjectionSubscriptionRequest> requests = subscriptions.Values
            .Select(s => s.Request)
            .ToImmutableList();

        return Task.FromResult(requests);
    }

    /// <inheritdoc />
    public Task OnActivateAsync(CancellationToken token)
    {
        string connectionId = this.GetPrimaryKeyString();
        Logger.SubscriptionGrainActivated(connectionId);

        // Note: Stream subscription rehydration will be added when stream logic is implemented.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string> SubscribeAsync(UxProjectionSubscriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string subscriptionId = Guid.NewGuid().ToString("N");
        Brooks.Abstractions.BrookKey brookKey = new(request.BrookType, request.EntityId);
        UxProjectionKey projectionKey = new(request.ProjectionType, brookKey);
        string connectionId = this.GetPrimaryKeyString();

        Logger.SubscribingToProjection(connectionId, subscriptionId, projectionKey);

        ActiveSubscription subscription = new()
        {
            Request = request,
            ProjectionKey = projectionKey,
            StreamHandle = null,
        };

        subscriptions[subscriptionId] = subscription;

        Logger.SubscribedToProjection(connectionId, subscriptionId, projectionKey);

        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(string subscriptionId)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);

        string connectionId = this.GetPrimaryKeyString();

        if (!subscriptions.TryGetValue(subscriptionId, out ActiveSubscription? subscription))
        {
            Logger.SubscriptionNotFound(connectionId, subscriptionId);
            return;
        }

        Logger.UnsubscribingFromProjection(connectionId, subscriptionId, subscription.ProjectionKey);

        if (subscription.StreamHandle is not null)
        {
            await subscription.StreamHandle.UnsubscribeAsync();
        }

        subscriptions.Remove(subscriptionId);

        Logger.UnsubscribedFromProjection(connectionId, subscriptionId, subscription.ProjectionKey);
    }
}
