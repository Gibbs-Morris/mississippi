using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Controls and records custom connection policy decisions for authentication regressions.
/// </summary>
internal sealed class DerivedConnectionPolicyEvaluator : PolicyEvaluator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DerivedConnectionPolicyEvaluator" /> class.
    /// </summary>
    /// <param name="authorization">The host's authorization service.</param>
    /// <param name="control">The evaluator decisions and invocation counts.</param>
    public DerivedConnectionPolicyEvaluator(
        IAuthorizationService authorization,
        ConnectionEvaluatorControl control
    )
        : base(authorization) =>
        Control = control;

    private ConnectionEvaluatorControl Control { get; }

    /// <inheritdoc />
    public override Task<AuthenticateResult> AuthenticateAsync(
        AuthorizationPolicy policy,
        HttpContext context
    )
    {
        Control.Authentications++;
        return Control.CanAuthenticate
            ? base.AuthenticateAsync(policy, context)
            : Task.FromResult(AuthenticateResult.NoResult());
    }

    /// <inheritdoc />
    public override Task<PolicyAuthorizationResult> AuthorizeAsync(
        AuthorizationPolicy policy,
        AuthenticateResult authenticationResult,
        HttpContext context,
        object? resource
    )
    {
        Control.Authorizations++;
        return Control.CanAuthorize
            ? base.AuthorizeAsync(policy, authenticationResult, context, resource)
            : Task.FromResult(PolicyAuthorizationResult.Forbid());
    }
}