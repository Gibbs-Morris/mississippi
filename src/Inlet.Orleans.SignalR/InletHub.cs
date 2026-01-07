using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Orleans.Grains;
using Mississippi.Viaduct.Grains;

using Orleans;


namespace Mississippi.Inlet.Orleans.SignalR;

/// <summary>
///     SignalR hub for managing projection subscriptions via Inlet.
/// </summary>
/// <remarks>
///     <para>
///         This hub provides a clean abstraction for clients to subscribe to projection
///         updates without needing to know about the underlying brook infrastructure.
///         Clients only provide projection type and entity ID - the server resolves
///         the brook mapping internally.
///     </para>
///     <para>
///         Each client connection gets a dedicated <see cref="IInletSubscriptionGrain" />
///         that manages all subscriptions for that connection, including brook stream
///         deduplication and fan-out on cursor move events.
///     </para>
/// </remarks>
public sealed class InletHub : Hub<IInletHubClient>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InletHub" /> class.
    /// </summary>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="logger">Logger instance for hub operations.</param>
    public InletHub(
        IGrainFactory grainFactory,
        ILogger<InletHub> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IGrainFactory GrainFactory { get; }

    private ILogger<InletHub> Logger { get; }

    private static string GetServerId() => Environment.MachineName;

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        Logger.ClientConnected(Context.ConnectionId);
        ISignalRClientGrain clientGrain = GrainFactory.GetGrain<ISignalRClientGrain>(Context.ConnectionId);
        await clientGrain.ConnectAsync(InletHubConstants.HubName, GetServerId());
        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(
        Exception? exception
    )
    {
        Logger.ClientDisconnected(Context.ConnectionId, exception);
        IInletSubscriptionGrain subscriptionGrain =
            GrainFactory.GetGrain<IInletSubscriptionGrain>(Context.ConnectionId);
        await subscriptionGrain.ClearAllAsync();
        ISignalRClientGrain clientGrain = GrainFactory.GetGrain<ISignalRClientGrain>(Context.ConnectionId);
        await clientGrain.DisconnectAsync();
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    ///     Subscribes to projection updates for an entity.
    /// </summary>
    /// <param name="projectionType">The type of projection to subscribe to.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The subscription identifier.</returns>
    /// <remarks>
    ///     The client does not need to know about brook details - the server
    ///     resolves the brook mapping from the projection type registry.
    /// </remarks>
    public async Task<string> SubscribeAsync(
        string projectionType,
        string entityId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        Logger.SubscribingToProjection(Context.ConnectionId, projectionType, entityId);
        string groupName = $"projection:{projectionType}:{entityId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        IInletSubscriptionGrain subscriptionGrain =
            GrainFactory.GetGrain<IInletSubscriptionGrain>(Context.ConnectionId);
        string subscriptionId = await subscriptionGrain.SubscribeAsync(projectionType, entityId);
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
    public async Task UnsubscribeAsync(
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
        IInletSubscriptionGrain subscriptionGrain =
            GrainFactory.GetGrain<IInletSubscriptionGrain>(Context.ConnectionId);
        await subscriptionGrain.UnsubscribeAsync(subscriptionId);
        Logger.UnsubscribedFromProjection(Context.ConnectionId, subscriptionId);
    }
}

/// <summary>
///     Strongly-typed client interface for the Inlet SignalR hub.
/// </summary>
public interface IInletHubClient
{
    /// <summary>
    ///     Called when a projection is updated.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="newVersion">The new version number.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProjectionUpdatedAsync(
        string projectionType,
        string entityId,
        long newVersion
    );
}