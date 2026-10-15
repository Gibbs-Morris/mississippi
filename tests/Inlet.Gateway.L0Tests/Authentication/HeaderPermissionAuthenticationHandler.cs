using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Reads current request permissions and keeps its injected host-scope identity visible.
/// </summary>
internal sealed class HeaderPermissionAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>,
      IAsyncDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="HeaderPermissionAuthenticationHandler" /> class.
    /// </summary>
    /// <param name="options">The scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    /// <param name="scope">The existing host scope and disposal observations.</param>
    /// <param name="factoryMarker">Configuration supplied only by the host factory.</param>
    public HeaderPermissionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ConnectionHandlerScope scope,
        string factoryMarker = "default"
    )
        : base(options, logger, encoder)
    {
        Scope = scope;
        FactoryMarker = factoryMarker;
    }

    /// <summary>
    ///     Gets a value indicating whether this handler has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    private string FactoryMarker { get; }

    private ConnectionHandlerScope Scope { get; }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        Scope.Disposals++;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string permission = Context.Request.Headers[Scheme.Name].ToString();
        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [
                    new(ClaimTypes.NameIdentifier, "header-user"), new("permission", permission),
                    new("tenant", Scope.Id),
                    new("factory", FactoryMarker),
                ],
                "Federation"));
        return Task.FromResult(AuthenticateResult.Success(new(principal, Scheme.Name)));
    }
}