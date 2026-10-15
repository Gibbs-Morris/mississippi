using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Uses the host-injected authentication service rather than resolving it from a decision context.
/// </summary>
internal sealed class InjectedConnectionPolicyEvaluator : IPolicyEvaluator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InjectedConnectionPolicyEvaluator" /> class.
    /// </summary>
    /// <param name="authentication">The authentication service injected by the host scope.</param>
    /// <param name="authorization">The host's authorization service.</param>
    public InjectedConnectionPolicyEvaluator(
        IAuthenticationService authentication,
        IAuthorizationService authorization
    )
    {
        Authentication = authentication;
        Evaluator = new(authorization);
    }

    private IAuthenticationService Authentication { get; }

    private PolicyEvaluator Evaluator { get; }

    /// <inheritdoc />
    public async Task<AuthenticateResult> AuthenticateAsync(
        AuthorizationPolicy policy,
        HttpContext context
    )
    {
        AuthenticateResult result = await Authentication.AuthenticateAsync(
            context,
            policy.AuthenticationSchemes.Distinct().Single());
        if (result.Principal is not null)
        {
            context.User = result.Principal;
        }

        return result;
    }

    /// <inheritdoc />
    public Task<PolicyAuthorizationResult> AuthorizeAsync(
        AuthorizationPolicy policy,
        AuthenticateResult authenticationResult,
        HttpContext context,
        object? resource
    ) =>
        Evaluator.AuthorizeAsync(policy, authenticationResult, context, resource);
}