using System;
using System.Collections.Generic;
using System.Linq;
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
        context.Items.Remove(ItemKey);
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
            // Items survive long-polling cloning; request features and their service scope do not.
            context.Items[ItemKey] = new Snapshot(
                result.Principal,
                Array.AsReadOnly(schemes),
                defaultScheme,
                OperatingSystem.IsWindows() && result.Principal.Identity is WindowsIdentity);
        }
    }

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
        context.Items.TryGetValue(ItemKey, out object? value) &&
        value is Snapshot snapshot &&
        ReferenceEquals(snapshot.Principal, principal) &&
        (snapshot.Schemes.SequenceEqual(policy.AuthenticationSchemes, StringComparer.Ordinal) ||
         ((policy.AuthenticationSchemes.Count == 1) &&
          string.Equals(snapshot.DefaultScheme, policy.AuthenticationSchemes[0], StringComparison.Ordinal)));

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

    /// <summary>
    ///     Associates verified schemes with the exact principal they authenticated.
    /// </summary>
    /// <param name="Principal">The authenticated principal.</param>
    /// <param name="Schemes">The original selected schemes.</param>
    /// <param name="DefaultScheme">The stable default scheme originally requested by middleware.</param>
    /// <param name="RequiresWindowsBinding">Whether SignalR's initial Windows clone must be bound at startup.</param>
    private sealed record Snapshot(
        ClaimsPrincipal Principal,
        IReadOnlyList<string> Schemes,
        string? DefaultScheme,
        bool RequiresWindowsBinding
    );
}