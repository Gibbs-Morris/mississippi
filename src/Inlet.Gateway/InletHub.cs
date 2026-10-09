using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Gateway.Authentication;
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

    // Weak keys keep per-context async gates from extending a connection's lifetime.
    private static ConditionalWeakTable<HttpContext, SemaphoreSlim> AuthenticationLocks { get; } = new();

    private IAuthorizationPolicyProvider AuthorizationPolicyProvider { get; }

    private IAuthorizationService AuthorizationService { get; }

    private IGrainFactory GrainFactory { get; }

    private InletServerOptions InletServerOptions { get; }

    private ILogger<InletHub> Logger { get; }

    private IProjectionAuthorizationRegistry ProjectionAuthorizationRegistry { get; }

    /// <summary>
    ///     Authenticates selected schemes and preserves custom evaluator authorization vetoes.
    /// </summary>
    /// <param name="policyEvaluator">The host's policy evaluator.</param>
    /// <param name="policy">The subscription policy.</param>
    /// <param name="httpContext">The connection's HTTP context.</param>
    /// <returns>The authenticated principal and whether the evaluator permits it.</returns>
    private static async Task<(ClaimsPrincipal? Principal, bool Permitted)> AuthenticateWithEvaluatorAsync(
        IPolicyEvaluator policyEvaluator,
        AuthorizationPolicy policy,
        HttpContext httpContext
    )
    {
        AuthenticateResult authenticationResult = await policyEvaluator.AuthenticateAsync(policy, httpContext);
        if (!authenticationResult.Succeeded)
        {
            return (null, false);
        }

        if (policyEvaluator.GetType() != typeof(PolicyEvaluator))
        {
            httpContext.User = authenticationResult.Principal;
            object? resource = AppContext.TryGetSwitch(
                                   "Microsoft.AspNetCore.Authorization.SuppressUseHttpContextAsAuthorizationResource",
                                   out bool useEndpoint) &&
                               useEndpoint
                ? httpContext.GetEndpoint()
                : httpContext;
            PolicyAuthorizationResult authorizationResult = await policyEvaluator.AuthorizeAsync(
                policy,
                authenticationResult,
                httpContext,
                resource);
            if (!authorizationResult.Succeeded)
            {
                return (authenticationResult.Principal, false);
            }
        }

        return (authenticationResult.Principal, true);
    }

    private static AuthenticationHandlerProvider? GetFrameworkHandlerProvider(
        IAuthenticationService? authenticationService
    ) =>
        authenticationService is AuthenticationService frameworkService &&
        (authenticationService.GetType().Assembly == typeof(AuthenticationService).Assembly) &&
        frameworkService.Handlers is AuthenticationHandlerProvider handlers &&
        (handlers.GetType() == typeof(AuthenticationHandlerProvider))
            ? handlers
            : null;

    private static string? GetUserId(
        ClaimsPrincipal? user
    ) =>
        user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.Identity?.Name;

    /// <summary>
    ///     Checks registrations required by the framework authentication path.
    /// </summary>
    /// <param name="authenticationService">The host's authentication service.</param>
    /// <param name="policy">The policy whose selected schemes will be authenticated.</param>
    /// <returns>Whether the required scheme registrations exist.</returns>
    private static async Task<bool> HasRequiredSchemeRegistrationsAsync(
        IAuthenticationService authenticationService,
        AuthorizationPolicy policy
    )
    {
        // Only the framework service and handler provider require scheme entries.
        // Custom services, handler providers and evaluators can authenticate virtual schemes.
        AuthenticationHandlerProvider? handlers = GetFrameworkHandlerProvider(authenticationService);
        if (handlers is not null)
        {
            foreach (string scheme in policy.AuthenticationSchemes)
            {
                AuthenticationScheme? registeredScheme = await handlers.Schemes.GetSchemeAsync(scheme);
                if (registeredScheme is null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override Task OnConnectedAsync()
    {
        ConnectionAuthenticationSnapshot.BindInitialWindowsClone(Context);

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

    private async Task<(ClaimsPrincipal? Principal, bool Permitted)> AuthenticateUserAsync(
        AuthorizationPolicy policy,
        HttpContext? httpContext,
        IPolicyEvaluator? policyEvaluator
    )
    {
        if (policy.AuthenticationSchemes.Count == 0)
        {
            return (Context.User ?? new ClaimsPrincipal(new ClaimsIdentity()), true);
        }

        if (httpContext is null)
        {
            return (null, false);
        }

        if (policyEvaluator is null)
        {
            return (null, false);
        }

        if (policyEvaluator.GetType() == typeof(PolicyEvaluator))
        {
            IAuthenticationService? authenticationService =
                httpContext.RequestServices.GetService<IAuthenticationService>();
            if (authenticationService is null)
            {
                return (null, false);
            }

            if (!await HasRequiredSchemeRegistrationsAsync(authenticationService, policy))
            {
                return (null, false);
            }

            if (ConnectionAuthenticationSnapshot.IsAuthenticatedForPolicy(httpContext, policy, Context.User))
            {
                // SignalR retains this principal for the connection, even when its HTTP features are reduced.
                return (Context.User, true);
            }

            if (GetFrameworkHandlerProvider(authenticationService) is not null &&
                ConnectionAuthenticationSnapshot.HasDifferentPrincipal(httpContext, Context.User))
            {
                // A reduced connection context still holds the first request's credentials.
                return (null, false);
            }
        }

        return await AuthenticateWithEvaluatorAsync(policyEvaluator, policy, httpContext);
    }

    private async Task AuthorizeAndRestoreAsync(
        AuthorizationPolicy policy,
        string path,
        string entityId,
        string? policyName,
        HttpContext? httpContext,
        IPolicyEvaluator? policyEvaluator
    )
    {
        HttpContext? authenticationContext = policy.AuthenticationSchemes.Count > 0 ? httpContext : null;
        ClaimsPrincipal? previousUser = authenticationContext?.User;
        IHttpAuthenticationFeature? previousHttpFeature =
            authenticationContext?.Features.Get<IHttpAuthenticationFeature>();
        IAuthenticateResultFeature? previousResultFeature =
            authenticationContext?.Features.Get<IAuthenticateResultFeature>();
        AuthenticateResult? previousResult = previousResultFeature?.AuthenticateResult;
        try
        {
            (ClaimsPrincipal? user, bool permitted) = await AuthenticateUserAsync(policy, httpContext, policyEvaluator);
            if (user is null || !permitted)
            {
                Logger.SubscriptionAuthorizationDenied(
                    Context.ConnectionId,
                    path,
                    entityId,
                    GetUserId(user),
                    policyName);
                throw new HubException(InletHubConstants.SubscriptionDeniedMessage);
            }

            IAuthorizationService authorizationService =
                authenticationContext?.RequestServices.GetService<IAuthorizationService>() ?? AuthorizationService;
            AuthorizationResult authorizationResult = await authorizationService.AuthorizeAsync(
                user,
                null,
                policy.Requirements);
            if (authorizationResult.Succeeded)
            {
                Logger.SubscriptionAuthorizationSucceeded(Context.ConnectionId, path, entityId, GetUserId(user));
                return;
            }

            Logger.SubscriptionAuthorizationDenied(Context.ConnectionId, path, entityId, GetUserId(user), policyName);
            throw new HubException(InletHubConstants.SubscriptionDeniedMessage);
        }
        finally
        {
            if (authenticationContext is not null)
            {
                // The selected user is needed during this decision, then the connection state resumes.
                // Restoring User alone clears ASP.NET's coupled authentication result.
                authenticationContext.Features.Set(previousHttpFeature);
                authenticationContext.User = previousUser!;
                authenticationContext.Features.Set(previousResultFeature);
                if (previousResultFeature is not null)
                {
                    previousResultFeature.AuthenticateResult = previousResult;
                }

                if (previousResult is null)
                {
                    authenticationContext.User = previousUser!;
                }
            }
        }
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
        HttpContext? httpContext = Context.GetHttpContext();
        if (httpContext is null)
        {
            await AuthorizeAndRestoreAsync(policy, path, entityId, policyName, null, null);
            return;
        }

        SemaphoreSlim authenticationLock = AuthenticationLocks.GetValue(httpContext, static _ => new(1, 1));
        await authenticationLock.WaitAsync(Context.ConnectionAborted);
        try
        {
            if (policy.AuthenticationSchemes.Count == 0)
            {
                await AuthorizeAndRestoreAsync(policy, path, entityId, policyName, httpContext, null);
                return;
            }

            IServiceProvider connectionServices = httpContext.RequestServices;
            IPolicyEvaluator? policyEvaluator = connectionServices.GetService<IPolicyEvaluator>();
            HttpContext decisionContext = SubscriptionAuthenticationFeatures.CreateContext(httpContext);
            AuthenticationService? frameworkService =
                connectionServices.GetService<IAuthenticationService>() as AuthenticationService;
            AuthenticationHandlerProvider? handlers = GetFrameworkHandlerProvider(frameworkService);
            if (handlers is not null)
            {
                decisionContext.RequestServices = new SubscriptionAuthenticationServices(
                    connectionServices,
                    frameworkService!,
                    handlers.Schemes);
            }

            await AuthorizeAndRestoreAsync(policy, path, entityId, policyName, decisionContext, policyEvaluator);
        }
        finally
        {
            authenticationLock.Release();
        }
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
            AuthorizationPolicy? defaultPolicy = await AuthorizationPolicyProvider.GetDefaultPolicyAsync();
            if (defaultPolicy is not null)
            {
                builder.Combine(defaultPolicy);
            }
            else
            {
                builder.RequireAuthenticatedUser();
            }
        }

        return builder.Build();
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