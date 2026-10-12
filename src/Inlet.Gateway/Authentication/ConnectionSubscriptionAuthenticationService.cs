using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Replays verified connection authentication through the framework service used by host evaluators.
/// </summary>
internal sealed class ConnectionSubscriptionAuthenticationService : IAuthenticationService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectionSubscriptionAuthenticationService" /> class.
    /// </summary>
    /// <param name="authentication">The fresh framework authentication service.</param>
    /// <param name="context">The isolated subscription decision context.</param>
    /// <param name="principalProvider">The established caller without eagerly caching SignalR's user.</param>
    public ConnectionSubscriptionAuthenticationService(
        AuthenticationService authentication,
        HttpContext context,
        Func<ClaimsPrincipal?> principalProvider
    )
    {
        FrameworkAuthentication = authentication;
        Context = context;
        PrincipalProvider = principalProvider;
    }

    /// <summary>
    ///     Gets the owned native service used to verify framework handler registrations.
    /// </summary>
    internal AuthenticationService FrameworkAuthentication { get; }

    private HttpContext Context { get; }

    private Func<ClaimsPrincipal?> PrincipalProvider { get; }

    /// <inheritdoc />
    public async Task<AuthenticateResult> AuthenticateAsync(
        HttpContext context,
        string? scheme
    )
    {
        string? requested = scheme ?? (await FrameworkAuthentication.Schemes.GetDefaultAuthenticateSchemeAsync())?.Name;
        if (ReferenceEquals(context, Context) &&
            requested is not null &&
            await FrameworkAuthentication.Schemes.GetSchemeAsync(requested) is not null &&
            ConnectionAuthenticationSnapshot.GetAuthenticationResult(context, PrincipalProvider(), requested) is
                AuthenticateResult established)
        {
            return established;
        }

        return await FrameworkAuthentication.AuthenticateAsync(context, scheme);
    }

    /// <inheritdoc />
    public Task ChallengeAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties
    ) =>
        FrameworkAuthentication.ChallengeAsync(context, scheme, properties);

    /// <inheritdoc />
    public Task ForbidAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties
    ) =>
        FrameworkAuthentication.ForbidAsync(context, scheme, properties);

    /// <inheritdoc />
    public Task SignInAsync(
        HttpContext context,
        string? scheme,
        ClaimsPrincipal principal,
        AuthenticationProperties? properties
    ) =>
        FrameworkAuthentication.SignInAsync(context, scheme, principal, properties);

    /// <inheritdoc />
    public Task SignOutAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties
    ) =>
        FrameworkAuthentication.SignOutAsync(context, scheme, properties);

    /// <summary>
    ///     Checks that every selected scheme has registered, caller-bound authentication available for replay.
    /// </summary>
    /// <param name="policy">The subscription policy.</param>
    /// <returns>Whether the evaluator can use verified authentication for the selected schemes.</returns>
    internal async Task<bool> CanUseEstablishedAuthenticationAsync(
        AuthorizationPolicy policy
    )
    {
        if (policy.AuthenticationSchemes.Count == 0)
        {
            return false;
        }

        ClaimsPrincipal? principal = PrincipalProvider();
        for (int index = 0; index < policy.AuthenticationSchemes.Count; index++)
        {
            string scheme = policy.AuthenticationSchemes[index];
            if (await FrameworkAuthentication.Schemes.GetSchemeAsync(scheme) is null)
            {
                return false;
            }
        }

        return policy.AuthenticationSchemes.All(scheme =>
            ConnectionAuthenticationSnapshot.GetAuthenticationResult(Context, principal, scheme) is
                { Succeeded: true });
    }
}