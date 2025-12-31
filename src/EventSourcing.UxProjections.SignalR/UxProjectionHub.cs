using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.Abstractions.Subscriptions;
using Mississippi.EventSourcing.UxProjections.SignalR.Grains;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.SignalR;

/// <summary>
///     SignalR hub for managing projection subscriptions.
/// </summary>
/// <remarks>
///     <para>
///         This hub uses a custom Orleans backplane where grains push directly
///         to clients via <see cref="IHubContext{THub}" />. Clients subscribe to
///         projection updates and receive notifications when projection versions change.
///     </para>
///     <para>
///         When a client subscribes, they are added to a SignalR group keyed by the
///         projection type and entity ID. Projection grains publish to these groups
///         when their state changes.
///     </para>
/// </remarks>
public sealed class UxProjectionHub : Hub<IUxProjectionHubClient>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionHub" /> class.
    /// </summary>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="logger">Logger instance for hub operations.</param>
    public UxProjectionHub(
        IGrainFactory grainFactory,
        ILogger<UxProjectionHub> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IGrainFactory GrainFactory { get; }

    private ILogger<UxProjectionHub> Logger { get; }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        Logger.ClientConnected(Context.ConnectionId);

        IUxClientGrain clientGrain = GrainFactory.GetGrain<IUxClientGrain>(Context.ConnectionId);
        await clientGrain.ConnectAsync(nameof(UxProjectionHub), GetServerId());

        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Logger.ClientDisconnected(Context.ConnectionId, exception);

        IUxProjectionSubscriptionGrain subscriptionGrain =
            GrainFactory.GetGrain<IUxProjectionSubscriptionGrain>(Context.ConnectionId);
        await subscriptionGrain.ClearAllAsync();

        IUxClientGrain clientGrain = GrainFactory.GetGrain<IUxClientGrain>(Context.ConnectionId);
        await clientGrain.DisconnectAsync();

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    ///     Subscribes to projection updates for an entity.
    /// </summary>
    /// <param name="projectionType">The type of projection to subscribe to.</param>
    /// <param name="brookType">The brook type for the projection.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The subscription identifier.</returns>
    public async Task<string> SubscribeToProjectionAsync(
        string projectionType,
        string brookType,
        string entityId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(brookType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        Logger.SubscribingToProjection(Context.ConnectionId, projectionType, entityId);

        // Add to SignalR group for direct projection grain notifications
        string groupName = $"projection:{projectionType}:{entityId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Track subscription in per-connection grain
        UxProjectionSubscriptionRequest request = new()
        {
            ProjectionType = projectionType,
            BrookType = brookType,
            EntityId = entityId,
            ClientSubscriptionId = Guid.NewGuid().ToString("N"),
        };

        IUxProjectionSubscriptionGrain grain =
            GrainFactory.GetGrain<IUxProjectionSubscriptionGrain>(Context.ConnectionId);
        string subscriptionId = await grain.SubscribeAsync(request);

        Logger.SubscribedToProjection(Context.ConnectionId, subscriptionId, projectionType, entityId);

        return subscriptionId;
    }

    /// <summary>
    ///     Unsubscribes from projection updates.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier returned from subscribe.</param>
    /// <param name="projectionType">The type of projection to unsubscribe from.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UnsubscribeFromProjectionAsync(
        string subscriptionId,
        string projectionType,
        string entityId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(subscriptionId);
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        Logger.UnsubscribingFromProjection(Context.ConnectionId, subscriptionId, projectionType, entityId);

        string groupName = $"projection:{projectionType}:{entityId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        IUxProjectionSubscriptionGrain grain =
            GrainFactory.GetGrain<IUxProjectionSubscriptionGrain>(Context.ConnectionId);
        await grain.UnsubscribeAsync(subscriptionId);

        Logger.UnsubscribedFromProjection(Context.ConnectionId, subscriptionId);
    }

    private static string GetServerId()
    {
        // Use a stable server identifier based on environment
        // In production, this would typically come from configuration
        return Environment.MachineName;
    }
}
