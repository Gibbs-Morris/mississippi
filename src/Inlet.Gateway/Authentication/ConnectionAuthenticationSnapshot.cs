using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Retains verified scheme provenance across SignalR's reduced connection context.
/// </summary>
internal static class ConnectionAuthenticationSnapshot
{
    private static object ItemKey { get; } = new();

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
        AuthenticateResult? result = context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult;
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

        if (ReferenceEquals(result.Principal, context.User))
        {
            // Items survive long-polling cloning; request features and their service scope do not.
            context.Items[ItemKey] = new Snapshot(result.Principal, Array.AsReadOnly(schemes));
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
        snapshot.Schemes.SequenceEqual(policy.AuthenticationSchemes, StringComparer.Ordinal);

    /// <summary>
    ///     Associates verified schemes with the exact principal they authenticated.
    /// </summary>
    /// <param name="Principal">The authenticated principal.</param>
    /// <param name="Schemes">The original selected schemes.</param>
    private sealed record Snapshot(ClaimsPrincipal Principal, IReadOnlyList<string> Schemes);
}