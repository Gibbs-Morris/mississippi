using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Spring.Server;

/// <summary>
///     Local development authentication handler used when optional Spring auth is enabled.
/// </summary>
public sealed class SpringLocalDevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringLocalDevAuthenticationHandler" /> class.
    /// </summary>
    /// <param name="options">Authentication scheme options monitor.</param>
    /// <param name="logger">Logger factory used by the authentication handler base class.</param>
    /// <param name="encoder">URL encoder used by the authentication handler base class.</param>
    /// <param name="springAuthOptions">Spring authentication options monitor.</param>
    public SpringLocalDevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<SpringAuthOptions> springAuthOptions
    )
        : base(options, logger, encoder)
    {
        ArgumentNullException.ThrowIfNull(springAuthOptions);
        SpringAuthOptions = springAuthOptions;
    }

    private IOptionsMonitor<SpringAuthOptions> SpringAuthOptions { get; }

    private static string[] ParseCommaSeparatedRoles(
        string? roles
    )
    {
        if (string.IsNullOrWhiteSpace(roles))
        {
            return [];
        }

        return roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        SpringAuthOptions options = SpringAuthOptions.CurrentValue;
        string userId = Request.Headers[options.UserHeader].FirstOrDefault() ?? options.DefaultUserId;
        string[] roles = ParseRoles(options);
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
        ];
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string[] ParseRoles(
        SpringAuthOptions options
    )
    {
        string? rawRoles = Request.Headers[options.RolesHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawRoles))
        {
            return ParseCommaSeparatedRoles(options.DefaultRoles);
        }

        string[] parsedRoles = rawRoles.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parsedRoles.Length == 0 ? ParseCommaSeparatedRoles(options.DefaultRoles) : parsedRoles;
    }
}