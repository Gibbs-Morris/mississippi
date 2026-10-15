using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Keeps injected framework authentication services aware of private subscription decisions.
/// </summary>
internal sealed class ConnectionAuthenticationRouter : IAuthenticationService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectionAuthenticationRouter" /> class.
    /// </summary>
    /// <param name="authentication">The host's configured native authentication service.</param>
    public ConnectionAuthenticationRouter(
        AuthenticationService authentication
    ) =>
        FrameworkAuthentication = authentication;

    /// <summary>
    ///     Gets the original framework service and its configured dependencies.
    /// </summary>
    internal AuthenticationService FrameworkAuthentication { get; }

    /// <summary>
    ///     Finds an owned framework service without treating host-defined authentication implementations as native.
    /// </summary>
    /// <param name="authentication">The configured service.</param>
    /// <returns>The original or decision-owned framework service, when available.</returns>
    internal static AuthenticationService? GetFrameworkAuthentication(
        IAuthenticationService? authentication
    ) =>
        authentication switch
        {
            ConnectionAuthenticationRouter router => router.FrameworkAuthentication,
            ConnectionSubscriptionAuthenticationService decision => decision.FrameworkAuthentication,
            AuthenticationService framework => framework,
            var _ => null,
        };

    /// <inheritdoc />
    public Task<AuthenticateResult> AuthenticateAsync(
        HttpContext context,
        string? scheme
    ) =>
        GetAuthentication(context).AuthenticateAsync(context, scheme);

    /// <inheritdoc />
    public Task ChallengeAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties
    ) =>
        GetAuthentication(context).ChallengeAsync(context, scheme, properties);

    /// <inheritdoc />
    public Task ForbidAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties
    ) =>
        GetAuthentication(context).ForbidAsync(context, scheme, properties);

    /// <inheritdoc />
    public Task SignInAsync(
        HttpContext context,
        string? scheme,
        ClaimsPrincipal principal,
        AuthenticationProperties? properties
    ) =>
        GetAuthentication(context).SignInAsync(context, scheme, principal, properties);

    /// <inheritdoc />
    public Task SignOutAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties
    ) =>
        GetAuthentication(context).SignOutAsync(context, scheme, properties);

    private IAuthenticationService GetAuthentication(
        HttpContext context
    ) =>
        context.Features.Get<ConnectionSubscriptionAuthenticationService>() is { } decision
            ? decision
            : FrameworkAuthentication;
}