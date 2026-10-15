using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Supplies independent per-scheme fixture results, requiring TLS for the certificate scheme.
/// </summary>
internal sealed class CompositeConnectionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CompositeConnectionAuthenticationHandler" /> class.
    /// </summary>
    /// <param name="options">The scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public CompositeConnectionAuthenticationHandler(
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
        string key = Scheme.Name + "-calls";
        Context.Items[key] = Context.Items.TryGetValue(key, out object? previous) && previous is int count
            ? count + 1
            : 1;
        string permission = Context.Request.Headers[Scheme.Name].ToString();
        if ((permission == "none") ||
            ((Scheme.Name == "Certificate") && Context.Features.Get<ITlsConnectionFeature>() is null))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [new(ClaimTypes.NameIdentifier, Scheme.Name), new("permission", permission)],
                "Federation"));
        return Task.FromResult(AuthenticateResult.Success(new(principal, Scheme.Name)));
    }
}