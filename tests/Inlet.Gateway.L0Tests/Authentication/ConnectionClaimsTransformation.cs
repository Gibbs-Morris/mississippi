using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Supplies custom no-op and identity-cloning transformations while observing repeated invocations.
/// </summary>
internal sealed class ConnectionClaimsTransformation : IClaimsTransformation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectionClaimsTransformation" /> class.
    /// </summary>
    /// <param name="cloneIdentities">Whether transformed identities are independent copies with an extra claim.</param>
    public ConnectionClaimsTransformation(
        bool cloneIdentities
    ) =>
        CloneIdentities = cloneIdentities;

    /// <summary>
    ///     Gets the number of transformations requested by the native authentication service.
    /// </summary>
    public int Calls { get; private set; }

    private bool CloneIdentities { get; }

    /// <inheritdoc />
    public Task<ClaimsPrincipal> TransformAsync(
        ClaimsPrincipal principal
    )
    {
        Calls++;
        if (!CloneIdentities)
        {
            return Task.FromResult(principal);
        }

        ClaimsPrincipal transformed = new(principal.Identities.Select(identity => new ClaimsIdentity(identity)));
        foreach (ClaimsIdentity identity in transformed.Identities)
        {
            identity.AddClaim(new("transformed", "yes"));
        }

        return Task.FromResult(transformed);
    }
}