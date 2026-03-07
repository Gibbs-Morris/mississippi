using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Mississippi.Spring.Gateway;

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

    private static bool IsTrue(
        string? value
    ) =>
        bool.TryParse(value, out bool parsed) && parsed;

    private static List<Claim> ParseClaims(
        string? claims
    )
    {
        if (string.IsNullOrWhiteSpace(claims))
        {
            return [];
        }

        string[] rawClaims = claims.Split(
            [';', ','],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<Claim> parsedClaims = [];
        foreach (string rawClaim in rawClaims)
        {
            string[] parts = rawClaim.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            string claimType = parts[0];
            string claimValue = parts[1];
            if (string.IsNullOrWhiteSpace(claimType) || string.IsNullOrWhiteSpace(claimValue))
            {
                continue;
            }

            parsedClaims.Add(new(claimType, claimValue));
        }

        return parsedClaims;
    }

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
        if (!options.Enabled)
        {
            Logger.LocalDevAuthDisabled(Scheme.Name);
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!IsLocalRequest())
        {
            Logger.LocalDevAuthRejectedNonLocalRequest(Scheme.Name);
            return Task.FromResult(
                AuthenticateResult.Fail("Spring local development authentication only allows local requests."));
        }

        if (IsAnonymousRequest(options))
        {
            Logger.LocalDevAuthAnonymousRequest(Scheme.Name);
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string userId = Request.Headers[options.UserHeader].FirstOrDefault() ?? options.DefaultUserId;
        string[] roles = ParseRoles(options);
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
        ];
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(ParseClaims(ParseClaimSource(options)));
        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);
        Logger.LocalDevAuthSucceeded(Scheme.Name, userId, roles.Length, claims.Count);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool IsAnonymousRequest(
        SpringAuthOptions options
    ) =>
        IsTrue(Request.Headers[options.AnonymousHeader].FirstOrDefault());

    private bool IsLocalRequest()
    {
        IPAddress? remoteAddress = Context.Connection.RemoteIpAddress;
        return remoteAddress is null || IPAddress.IsLoopback(remoteAddress);
    }

    private string? ParseClaimSource(
        SpringAuthOptions options
    )
    {
        string? rawClaims = Request.Headers[options.ClaimsHeader].FirstOrDefault();
        return string.IsNullOrWhiteSpace(rawClaims) ? options.DefaultClaims : rawClaims;
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

        if (string.Equals(rawRoles, "none", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        string[] parsedRoles = rawRoles.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parsedRoles.Length == 0 ? ParseCommaSeparatedRoles(options.DefaultRoles) : parsedRoles;
    }
}