using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Reads fixture permissions from the current TLS certificate's public subject.
/// </summary>
internal sealed class CertificatePermissionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CertificatePermissionAuthenticationHandler" /> class.
    /// </summary>
    /// <param name="options">The named options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public CertificatePermissionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    )
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        X509Certificate2? certificate = Context.Features.Get<ITlsConnectionFeature>()?.ClientCertificate;
        if (certificate is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [
                    new(ClaimTypes.NameIdentifier, "certificate-user"),
                    new("permission", certificate.GetNameInfo(X509NameType.SimpleName, false)),
                ],
                "Federation"));
        return Task.FromResult(AuthenticateResult.Success(new(principal, Scheme.Name)));
    }
}