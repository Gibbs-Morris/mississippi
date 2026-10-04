using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace MississippiSamples.Spring.Client.AuthSimulation;

/// <summary>
///     Applies local-auth simulation headers to outgoing requests.
/// </summary>
/// <remarks>
///     <para>
///         This handler holds a local copy of the auth simulation profile that is
///         pushed by <see cref="AuthSimulationSyncEffect" /> whenever the store
///         processes a <c>SetAuthSimulationProfileAction</c>.
///     </para>
///     <para>
///         The handler intentionally does <strong>not</strong> depend on <c>IStore</c>.
///         Injecting the store directly would create a circular DI chain because
///         <c>HttpClient → this handler → IStore → InletSignalRActionEffect → AutoProjectionFetcher → HttpClient</c>.
///         Using an effect as a bridge breaks the cycle.
///     </para>
/// </remarks>
public sealed class AuthSimulationHeadersHandler : DelegatingHandler
{
    private const string AnonymousHeaderName = "X-Spring-Anonymous";

    private const string ClaimsHeaderName = "X-Spring-Claims";

    private const string RolesHeaderName = "X-Spring-Roles";

    private AuthSimulationProfileSnapshot currentProfile = new(
        false,
        "banking-operator,transfer-operator,auth-proof-operator",
        "spring.permission=auth-proof");

    /// <summary>
    ///     Gets the claims header value applied to outgoing requests.
    /// </summary>
    public string? Claims => Volatile.Read(ref currentProfile).Claims;

    /// <summary>
    ///     Gets a value indicating whether outgoing requests should force anonymous identity.
    /// </summary>
    public bool IsAnonymous => Volatile.Read(ref currentProfile).IsAnonymous;

    /// <summary>
    ///     Gets the roles header value applied to outgoing requests.
    /// </summary>
    public string? Roles => Volatile.Read(ref currentProfile).Roles;

    /// <summary>
    ///     Updates the auth simulation profile applied to subsequent outgoing requests.
    /// </summary>
    /// <param name="isAnonymous">Whether requests should force anonymous behavior.</param>
    /// <param name="roles">Role header value.</param>
    /// <param name="claims">Claim header value.</param>
    public void SetProfile(
        bool isAnonymous,
        string? roles,
        string? claims
    )
    {
        Interlocked.Exchange(ref currentProfile, new(isAnonymous, roles, claims));
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ApplyHeaders(request);
        return base.SendAsync(request, cancellationToken);
    }

    private void ApplyHeaders(
        HttpRequestMessage request
    )
    {
        AuthSimulationProfileSnapshot profile = Volatile.Read(ref currentProfile);
        request.Headers.Remove(AnonymousHeaderName);
        request.Headers.Remove(RolesHeaderName);
        request.Headers.Remove(ClaimsHeaderName);
        if (profile.IsAnonymous)
        {
            request.Headers.Add(AnonymousHeaderName, "true");
            return;
        }

        if (!string.IsNullOrWhiteSpace(profile.Roles))
        {
            request.Headers.Add(RolesHeaderName, profile.Roles);
        }

        if (!string.IsNullOrWhiteSpace(profile.Claims))
        {
            request.Headers.Add(ClaimsHeaderName, profile.Claims);
        }
    }

    private sealed record AuthSimulationProfileSnapshot(bool IsAnonymous, string? Roles, string? Claims);
}