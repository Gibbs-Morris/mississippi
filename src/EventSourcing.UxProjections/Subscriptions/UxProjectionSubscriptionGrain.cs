using System;
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
///         Stream subscription handles are not serializable and are rehydrated on grain activation
///         by re-subscribing to the projection streams from persisted state.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.UxProjectionSubscriptionGrain")]
internal sealed class UxProjectionSubscriptionGrain : IUxProjectionSubscriptionGrain, IGrainBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionSubscriptionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">Orleans grain context for this grain instance.</param>
    /// <param name="streamProviderOptions">Configuration options for the Orleans stream provider.</param>
    /// <param name="streamIdFactory">Factory for creating Orleans stream identifiers.</param>
    /// <param name="state">Persistent state for storing active subscriptions.</param>
    /// <param name="logger">Logger instance for logging subscription grain operations.</param>
    public UxProjectionSubscriptionGrain(
        IGrainContext grainContext,
        IOptions<BrookProviderOptions> streamProviderOptions,
        IStreamIdFactory streamIdFactory,
        [PersistentState("subscriptions", "UxProjectionStore")]
        IPersistentState<UxProjectionSubscriptionState> state,
        ILogger<UxProjectionSubscriptionGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        StreamProviderOptions = streamProviderOptions ?? throw new ArgumentNullException(nameof(streamProviderOptions));
        StreamIdFactory = streamIdFactory ?? throw new ArgumentNullException(nameof(streamIdFactory));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<UxProjectionSubscriptionGrain> Logger { get; }

    private IPersistentState<UxProjectionSubscriptionState> State { get; }

    private IStreamIdFactory StreamIdFactory { get; }

    private IOptions<BrookProviderOptions> StreamProviderOptions { get; }

    /// <inheritdoc />
    public async Task ClearAllAsync()
    {
        string connectionId = this.GetPrimaryKeyString();
        Logger.ClearingAllSubscriptions(connectionId, State.State.Subscriptions.Count);

        foreach (ActiveSubscription subscription in State.State.Subscriptions.Values
            .Where(s => s.StreamHandle is not null))
        {
            await subscription.StreamHandle!.UnsubscribeAsync();
        }

        State.State.Subscriptions.Clear();
        await State.WriteStateAsync();

        Logger.AllSubscriptionsCleared(connectionId);

        this.DeactivateOnIdle();
    }

    /// <inheritdoc />
    public Task<ImmutableList<UxProjectionSubscriptionRequest>> GetSubscriptionsAsync()
    {
        ImmutableList<UxProjectionSubscriptionRequest> requests = State.State.Subscriptions.Values
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
    public async Task<string> SubscribeAsync(UxProjectionSubscriptionRequest request)
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

        State.State.Subscriptions[subscriptionId] = subscription;
        await State.WriteStateAsync();

        Logger.SubscribedToProjection(connectionId, subscriptionId, projectionKey);

        return subscriptionId;
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(string subscriptionId)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);

        string connectionId = this.GetPrimaryKeyString();

        if (!State.State.Subscriptions.TryGetValue(subscriptionId, out ActiveSubscription? subscription))
        {
            Logger.SubscriptionNotFound(connectionId, subscriptionId);
            return;
        }

        Logger.UnsubscribingFromProjection(connectionId, subscriptionId, subscription.ProjectionKey);

        if (subscription.StreamHandle is not null)
        {
            await subscription.StreamHandle.UnsubscribeAsync();
        }

        State.State.Subscriptions.Remove(subscriptionId);
        await State.WriteStateAsync();

        Logger.UnsubscribedFromProjection(connectionId, subscriptionId, subscription.ProjectionKey);
    }
}
