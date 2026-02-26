using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Inlet.Runtime.Grains;

using Orleans;


namespace Mississippi.Inlet.Gateway;

/// <summary>
///     SignalR hub for managing projection subscriptions via Inlet.
/// </summary>
/// <remarks>
///     <para>
///         This hub provides a clean abstraction for clients to subscribe to projection
///         updates without needing to know about the underlying brook infrastructure.
///         Clients only provide projection path and entity ID - the server resolves
///         the brook mapping internally.
///     </para>
///     <para>
///         Each client connection gets a dedicated <see cref="IInletSubscriptionGrain" />
///         that manages all subscriptions for that connection, including brook stream
///         deduplication and fan-out on cursor move events. The subscription grain
///         sends notifications directly to the client via the SignalR client grain.
///     </para>
/// </remarks>
public sealed class InletHub : Hub<IInletHubClient>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InletHub" /> class.
    /// </summary>
    /// <param name="grainFactory">Factory for creating grain references.</param>
    /// <param name="projectionAuthorizationRegistry">Registry of projection authorization metadata.</param>
    /// <param name="authorizationService">Authorization service for evaluating projection subscription policies.</param>
    /// <param name="authorizationPolicyProvider">Authorization policy provider for resolving named policies.</param>
    /// <param name="inletServerOptions">The current Inlet server options.</param>
    /// <param name="logger">Logger instance for hub operations.</param>
    public InletHub(
        IGrainFactory grainFactory,
        IProjectionAuthorizationRegistry projectionAuthorizationRegistry,
        IAuthorizationService authorizationService,
        IAuthorizationPolicyProvider authorizationPolicyProvider,
        IOptions<InletServerOptions> inletServerOptions,
        ILogger<InletHub> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        ProjectionAuthorizationRegistry = projectionAuthorizationRegistry ??
                                          throw new ArgumentNullException(nameof(projectionAuthorizationRegistry));
        AuthorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        AuthorizationPolicyProvider = authorizationPolicyProvider ??
                                      throw new ArgumentNullException(nameof(authorizationPolicyProvider));
        InletServerOptions = inletServerOptions?.Value ?? throw new ArgumentNullException(nameof(inletServerOptions));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IAuthorizationPolicyProvider AuthorizationPolicyProvider { get; }

    private IAuthorizationService AuthorizationService { get; }

    private IGrainFactory GrainFactory { get; }

    private InletServerOptions InletServerOptions { get; }

    private ILogger<InletHub> Logger { get; }

    private IProjectionAuthorizationRegistry ProjectionAuthorizationRegistry { get; }

    /// <inheritdoc />
    public override Task OnConnectedAsync()
    {
        // Note: Client grain registration is handled by AqueductHubLifetimeManager.
        // We just log the connection here.
        Logger.ClientConnected(Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(
        Exception? exception
    )
    {
        // Note: Client grain disconnect is handled by AqueductHubLifetimeManager.
        // We just clean up the subscription grain here.
        Logger.ClientDisconnected(Context.ConnectionId, exception);
        IInletSubscriptionGrain subscriptionGrain =
            GrainFactory.GetGrain<IInletSubscriptionGrain>(Context.ConnectionId);
        await subscriptionGrain.ClearAllAsync();
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    ///     Subscribes to projection updates for an entity.
    /// </summary>
    /// <param name="path">The projection path (e.g., "chat/channels").</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The subscription identifier.</returns>
    /// <remarks>
    ///     The client does not need to know about brook details - the server
    ///     resolves the brook mapping from the projection path registry.
    /// </remarks>
    public async Task<string> SubscribeAsync(
        string path,
        string entityId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        await AuthorizeSubscriptionAsync(path, entityId);
        Logger.SubscribingToProjection(Context.ConnectionId, path, entityId);
        IInletSubscriptionGrain subscriptionGrain =
            GrainFactory.GetGrain<IInletSubscriptionGrain>(Context.ConnectionId);
        string subscriptionId = await subscriptionGrain.SubscribeAsync(path, entityId);
        Logger.SubscribedToProjection(Context.ConnectionId, subscriptionId, path, entityId);
        return subscriptionId;
    }

    /// <summary>
    ///     Unsubscribes from projection updates.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier returned from subscribe.</param>
    /// <param name="path">The projection path to unsubscribe from.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UnsubscribeAsync(
        string subscriptionId,
        string path,
        string entityId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(subscriptionId);
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        Logger.UnsubscribingFromProjection(Context.ConnectionId, subscriptionId, path, entityId);
        IInletSubscriptionGrain subscriptionGrain =
            GrainFactory.GetGrain<IInletSubscriptionGrain>(Context.ConnectionId);
        await subscriptionGrain.UnsubscribeAsync(subscriptionId);
        Logger.UnsubscribedFromProjection(Context.ConnectionId, subscriptionId);
    }

    private async Task AuthorizeSubscriptionAsync(
        string path,
        string entityId
    )
    {
        GeneratedApiAuthorizationOptions authorizationOptions = InletServerOptions.GeneratedApiAuthorization;
        ProjectionAuthorizationMetadata? metadata = ProjectionAuthorizationRegistry.GetAuthorizationMetadata(path);
        if (metadata is not null && metadata.HasAllowAnonymous && authorizationOptions.AllowAnonymousOptOut)
        {
            Logger.SubscriptionAuthorizationSkipped(Context.ConnectionId, path, entityId, "AllowAnonymous");
            return;
        }

        if (metadata is not null && metadata.HasAuthorize)
        {
            await AuthorizeWithPolicyAsync(
                await BuildAuthorizationPolicyAsync(metadata.Policy, metadata.Roles, metadata.AuthenticationSchemes),
                path,
                entityId,
                metadata.Policy);
            return;
        }

        if (authorizationOptions.Mode != GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints)
        {
            Logger.SubscriptionAuthorizationSkipped(Context.ConnectionId, path, entityId, "AuthorizationModeDisabled");
            return;
        }

        await AuthorizeWithPolicyAsync(
            await BuildAuthorizationPolicyAsync(
                authorizationOptions.DefaultPolicy,
                authorizationOptions.DefaultRoles,
                authorizationOptions.DefaultAuthenticationSchemes),
            path,
            entityId,
            authorizationOptions.DefaultPolicy);
    }

    private async Task AuthorizeWithPolicyAsync(
        AuthorizationPolicy policy,
        string path,
        string entityId,
        string? policyName
    )
    {
        ClaimsPrincipal user = Context.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        AuthorizationResult authorizationResult = await AuthorizationService.AuthorizeAsync(
            user,
            null,
            policy.Requirements);
        if (authorizationResult.Succeeded)
        {
            Logger.SubscriptionAuthorizationSucceeded(Context.ConnectionId, path, entityId, GetUserId());
            return;
        }

        Logger.SubscriptionAuthorizationDenied(Context.ConnectionId, path, entityId, GetUserId(), policyName);
        throw new HubException(InletHubConstants.SubscriptionDeniedMessage);
    }

    private async Task<AuthorizationPolicy> BuildAuthorizationPolicyAsync(
        string? policy,
        string? roles,
        string? authenticationSchemes
    )
    {
        AuthorizationPolicyBuilder builder = new();
        bool hasRequirements = false;
        if (!string.IsNullOrWhiteSpace(policy))
        {
            AuthorizationPolicy? namedPolicy = await AuthorizationPolicyProvider.GetPolicyAsync(policy);
            if (namedPolicy is null)
            {
                return new AuthorizationPolicyBuilder().RequireAssertion(static _ => false).Build();
            }

            builder.Combine(namedPolicy);
            hasRequirements = namedPolicy.Requirements.Count > 0;
        }

        if (!string.IsNullOrWhiteSpace(authenticationSchemes))
        {
            foreach (string scheme in authenticationSchemes.Split(
                         ',',
                         StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                builder.AuthenticationSchemes.Add(scheme);
            }
        }

        if (!string.IsNullOrWhiteSpace(roles))
        {
            string[] splitRoles = roles.Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (splitRoles.Length > 0)
            {
                builder.RequireRole(splitRoles);
                hasRequirements = true;
            }
        }

        if (!hasRequirements)
        {
            builder.RequireAuthenticatedUser();
        }

        return builder.Build();
    }

    private string? GetUserId() =>
        Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.Identity?.Name;
}

/// <summary>
///     Strongly-typed client interface for the Inlet SignalR hub.
/// </summary>
public interface IInletHubClient
{
    /// <summary>
    ///     Called when a projection is updated.
    /// </summary>
    /// <param name="path">The projection path.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="newVersion">The new version number.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProjectionUpdatedAsync(
        string path,
        string entityId,
        long newVersion
    );
}