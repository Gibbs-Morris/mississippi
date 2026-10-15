using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Retains verified scheme provenance across SignalR's reduced connection context.
/// </summary>
internal static class ConnectionAuthenticationSnapshot
{
    private static object ItemKey { get; } = new();

    private static ConditionalWeakTable<ClaimsPrincipal, Snapshot> RequestSnapshots { get; } = new();

    /// <summary>
    ///     Binds SignalR's initial Windows identity clone before the connection accepts invocations.
    /// </summary>
    /// <param name="caller">The newly established hub connection.</param>
    internal static void BindInitialWindowsClone(
        HubCallerContext caller
    )
    {
        HttpContext? context = caller.GetHttpContext();
        if (context is null ||
            !context.Items.TryGetValue(ItemKey, out object? value) ||
            value is not Snapshot { RequiresWindowsBinding: true } snapshot)
        {
            return;
        }

        // Consume the startup binding once, even if a host has replaced the Windows principal.
        // Other identities must keep SignalR's existing lazy principal caching behavior.
        Snapshot bound = snapshot with
        {
            RequiresWindowsBinding = false,
        };
        if (OperatingSystem.IsWindows() && caller.User is { Identity: WindowsIdentity } principal)
        {
            bound = bound with
            {
                Principal = principal,
            };
        }

        context.Items[ItemKey] = bound;
    }

    /// <summary>
    ///     Captures the schemes that authenticated the principal handed to SignalR.
    /// </summary>
    /// <param name="context">The original connection request after authentication.</param>
    /// <returns>A task that completes when scheme provenance has been captured.</returns>
    internal static async Task CaptureAsync(
        HttpContext context
    )
    {
        // Retain the first caller even when middleware did not authenticate it. Empty schemes
        // record caller ownership only and cannot establish successful authentication.
        StoreSnapshot(context, context.User, [], null);
        IAuthenticateResultFeature? feature = context.Features.Get<IAuthenticateResultFeature>();
        AuthenticateResult? result = feature?.AuthenticateResult;
        if (result is not { Succeeded: true, Ticket: not null } || !ReferenceEquals(result.Principal, context.User))
        {
            return;
        }

        string ticketScheme = result.Ticket.AuthenticationScheme;
        string[] schemes = [ticketScheme];
        if (ticketScheme.Contains(';', StringComparison.Ordinal))
        {
            // ASP.NET joins scheme names without escaping literal semicolons. Recover the original list
            // only when its provider permits policy caching; a later dynamic policy may differ.
            IAuthorizationPolicyProvider? provider = context.RequestServices.GetService<IAuthorizationPolicyProvider>();
            if (provider is null || !provider.AllowsCachingPolicies)
            {
                return;
            }

            Endpoint? endpoint = context.GetEndpoint();
            AuthorizationPolicy? policy = await AuthorizationPolicy.CombineAsync(
                provider,
                endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>() ?? [],
                endpoint?.Metadata.GetOrderedMetadata<AuthorizationPolicy>() ?? []);
            if (policy is { AuthenticationSchemes.Count: > 0 })
            {
                if (!string.Equals(
                        ticketScheme,
                        string.Join(';', policy.AuthenticationSchemes),
                        StringComparison.Ordinal))
                {
                    return;
                }

                schemes = policy.AuthenticationSchemes.ToArray();
            }
        }

        string? defaultScheme = await GetDefaultAuthenticationSchemeAsync(context, feature!);
        if (ReferenceEquals(result.Principal, context.User) && ReferenceEquals(result, feature!.AuthenticateResult))
        {
            StoreSnapshot(context, result.Principal, schemes, defaultScheme, result.Properties);
        }
    }

    /// <summary>
    ///     Recovers a verified single-scheme result for the exact established caller.
    /// </summary>
    /// <param name="context">The isolated connection decision context.</param>
    /// <param name="principal">The principal retained by the hub.</param>
    /// <param name="scheme">The scheme requested by the evaluator.</param>
    /// <returns>The established result when its scheme or stable default alias matches.</returns>
    internal static AuthenticateResult? GetAuthenticationResult(
        HttpContext context,
        ClaimsPrincipal? principal,
        string scheme
    )
    {
        Snapshot? snapshot = GetSnapshot(context, principal);
        if (snapshot is not { Schemes.Count: 1 } ||
            (!string.Equals(snapshot.Schemes[0], scheme, StringComparison.Ordinal) &&
             !string.Equals(snapshot.DefaultScheme, scheme, StringComparison.Ordinal)))
        {
            return null;
        }

        return AuthenticateResult.Success(new(snapshot.Principal, snapshot.Properties?.Clone(), snapshot.Schemes[0]));
    }

    /// <summary>
    ///     Checks whether retained request credentials belong to a different caller principal.
    /// </summary>
    /// <param name="context">The connection context, including its copied items.</param>
    /// <param name="principal">The principal retained by the hub.</param>
    /// <returns>Whether a recorded result belongs to a different principal.</returns>
    internal static bool HasDifferentPrincipal(
        HttpContext context,
        ClaimsPrincipal? principal
    ) =>
        context.Items.TryGetValue(ItemKey, out object? value) &&
        value is Snapshot snapshot &&
        !ReferenceEquals(snapshot.Principal, principal);

    /// <summary>
    ///     Checks that the connection authenticated the subscription's exact selected schemes.
    /// </summary>
    /// <param name="context">The connection context, including its copied items.</param>
    /// <param name="policy">The subscription policy.</param>
    /// <param name="principal">The principal retained by the hub.</param>
    /// <returns>Whether the established authentication matches the selected schemes.</returns>
    internal static bool IsAuthenticatedForPolicy(
        HttpContext context,
        AuthorizationPolicy policy,
        ClaimsPrincipal? principal
    ) =>
        GetSnapshot(context, principal) is Snapshot snapshot &&
        (snapshot.Schemes.Count > 0) &&
        (snapshot.Schemes.SequenceEqual(policy.AuthenticationSchemes, StringComparer.Ordinal) ||
         ((policy.AuthenticationSchemes.Count == 1) &&
          string.Equals(snapshot.DefaultScheme, policy.AuthenticationSchemes[0], StringComparison.Ordinal)));

    /// <summary>
    ///     Applies credentials from a separately proven anonymous poll to a private decision context.
    /// </summary>
    /// <param name="context">The isolated subscription authentication context.</param>
    /// <param name="principal">The principal retained by the hub.</param>
    /// <returns>Whether a current anonymous poll supplied the decision's request metadata.</returns>
    internal static bool TryUseAnonymousPollRequest(
        HttpContext context,
        ClaimsPrincipal? principal
    )
    {
        Snapshot? current = GetSnapshot(context, principal);
        if (current is not { Schemes.Count: 0, AnonymousRequest: not null } ||
            principal is null ||
            principal.Identities.Any(identity => identity.IsAuthenticated) ||
            !context.Items.TryGetValue(ItemKey, out object? value) ||
            value is not Snapshot { AnonymousRequest: not null } original ||
            original.Principal.Identities.Any(identity => identity.IsAuthenticated))
        {
            return false;
        }

        context.Features.Set(current.AnonymousRequest.CreateFeature());
        return true;
    }

    /// <summary>
    ///     Recovers the stable default scheme requested by authentication middleware.
    /// </summary>
    /// <param name="context">The original authenticated request.</param>
    /// <param name="feature">The verified authentication result feature.</param>
    /// <returns>The requested default scheme when no endpoint policy selected other schemes.</returns>
    private static async Task<string?> GetDefaultAuthenticationSchemeAsync(
        HttpContext context,
        IAuthenticateResultFeature feature
    )
    {
        // Middleware couples its result and HTTP user. Separate result features do not prove
        // that the default scheme was requested, and custom providers may change their default.
        if (!ReferenceEquals(feature, context.Features.Get<IHttpAuthenticationFeature>()) ||
            context.RequestServices.GetService<IAuthenticationSchemeProvider>() is not AuthenticationSchemeProvider
                schemes ||
            (schemes.GetType() != typeof(AuthenticationSchemeProvider)))
        {
            return null;
        }

        IAuthorizationPolicyProvider? provider = context.RequestServices.GetService<IAuthorizationPolicyProvider>();
        if (provider is null || !provider.AllowsCachingPolicies)
        {
            return null;
        }

        Endpoint? endpoint = context.GetEndpoint();
        AuthorizationPolicy? policy = await AuthorizationPolicy.CombineAsync(
            provider,
            endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>() ?? [],
            endpoint?.Metadata.GetOrderedMetadata<AuthorizationPolicy>() ?? []);
        return policy is { AuthenticationSchemes.Count: > 0 }
            ? null
            : (await schemes.GetDefaultAuthenticateSchemeAsync())?.Name;
    }

    private static Snapshot? GetSnapshot(
        HttpContext context,
        ClaimsPrincipal? principal
    )
    {
        if (!context.Items.TryGetValue(ItemKey, out object? value) || value is not Snapshot original)
        {
            return null;
        }

        if (ReferenceEquals(original.Principal, principal))
        {
            return original;
        }

        return principal is not null &&
               original.Endpoint is not null &&
               ReferenceEquals(original.Endpoint, context.GetEndpoint()) &&
               (original.ConnectionToken.Length > 0) &&
               RequestSnapshots.TryGetValue(principal, out Snapshot? current) &&
               ReferenceEquals(current.Endpoint, original.Endpoint) &&
               string.Equals(current.ConnectionToken, original.ConnectionToken, StringComparison.Ordinal)
            ? current
            : null;
    }

    /// <summary>
    ///     Stores caller ownership and registers poll provenance only for verified schemes.
    /// </summary>
    /// <param name="context">The original authenticated request.</param>
    /// <param name="principal">The principal handed to SignalR by the original request.</param>
    /// <param name="schemes">The verified schemes, or an empty list when no authentication is proven.</param>
    /// <param name="defaultScheme">The stable default scheme originally requested by middleware.</param>
    /// <param name="properties">The verified ticket properties, or null for caller ownership only.</param>
    private static void StoreSnapshot(
        HttpContext context,
        ClaimsPrincipal principal,
        string[] schemes,
        string? defaultScheme,
        AuthenticationProperties? properties = null
    )
    {
        // Items survive long-polling cloning; request features and their service scope do not.
        ConnectionAnonymousRequest? anonymousRequest = principal.Identities.Any(identity => identity.IsAuthenticated)
            ? null
            : new(context.Request);
        Snapshot snapshot = new(
            principal,
            Array.AsReadOnly(schemes),
            defaultScheme,
            OperatingSystem.IsWindows() && principal.Identity is WindowsIdentity,
            context.GetEndpoint(),
            context.Request.Query["id"].ToString(),
            properties?.Clone(),
            anonymousRequest);
        context.Items[ItemKey] = snapshot;
        if (((schemes.Length > 0) || snapshot.AnonymousRequest is not null) &&
            snapshot.Endpoint is not null &&
            (snapshot.ConnectionToken.Length > 0))
        {
            // A later poll can supply the principal first read by the hub during its handshake.
            // Bind its verified schemes to this exact principal, mapped endpoint and transport token.
            RequestSnapshots.AddOrUpdate(principal, snapshot);
        }
    }

    /// <summary>
    ///     Associates verified schemes with the exact principal they authenticated.
    /// </summary>
    /// <param name="Principal">The authenticated principal.</param>
    /// <param name="Schemes">The original selected schemes.</param>
    /// <param name="DefaultScheme">The stable default scheme originally requested by middleware.</param>
    /// <param name="RequiresWindowsBinding">Whether SignalR's initial Windows clone must be bound at startup.</param>
    /// <param name="Endpoint">The mapped endpoint that accepted the authentication result.</param>
    /// <param name="ConnectionToken">The transport token from the authenticated request.</param>
    /// <param name="Properties">A copy of the verified ticket properties, when authentication succeeded.</param>
    /// <param name="AnonymousRequest">Copied request metadata when middleware handed SignalR an anonymous caller.</param>
    private sealed record Snapshot(
        ClaimsPrincipal Principal,
        IReadOnlyList<string> Schemes,
        string? DefaultScheme,
        bool RequiresWindowsBinding,
        Endpoint? Endpoint,
        string ConnectionToken,
        AuthenticationProperties? Properties,
        ConnectionAnonymousRequest? AnonymousRequest
    );
}