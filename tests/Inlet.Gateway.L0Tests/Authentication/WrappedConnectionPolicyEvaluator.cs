using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Controls and records custom connection policy decisions for authentication regressions.
/// </summary>
internal sealed class WrappedConnectionPolicyEvaluator : IPolicyEvaluator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="WrappedConnectionPolicyEvaluator" /> class.
    /// </summary>
    /// <param name="authorization">The host's authorization service.</param>
    /// <param name="control">The evaluator decisions and invocation counts.</param>
    public WrappedConnectionPolicyEvaluator(
        IAuthorizationService authorization,
        ConnectionEvaluatorControl control
    ) =>
        Evaluator = new(authorization, control);

    private DerivedConnectionPolicyEvaluator Evaluator { get; }

    /// <inheritdoc />
    public Task<AuthenticateResult> AuthenticateAsync(
        AuthorizationPolicy policy,
        HttpContext context
    ) =>
        Evaluator.AuthenticateAsync(policy, context);

    /// <inheritdoc />
    public Task<PolicyAuthorizationResult> AuthorizeAsync(
        AuthorizationPolicy policy,
        AuthenticateResult authenticationResult,
        HttpContext context,
        object? resource
    ) =>
        Evaluator.AuthorizeAsync(policy, authenticationResult, context, resource);
}