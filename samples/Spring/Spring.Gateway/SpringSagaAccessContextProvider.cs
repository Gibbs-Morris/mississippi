using System;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;


namespace MississippiSamples.Spring.Gateway;

/// <summary>
///     Derives a stable saga-access fingerprint from the authenticated Spring gateway caller.
/// </summary>
internal sealed class SpringSagaAccessContextProvider : ISagaAccessContextProvider
{
    /// <summary>
    ///     Request-context key used to flow the caller fingerprint from the gateway into Orleans grain calls.
    /// </summary>
    internal const string RequestContextKey = "spring.saga.access-fingerprint";

    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringSagaAccessContextProvider" /> class.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
    public SpringSagaAccessContextProvider(
        IHttpContextAccessor httpContextAccessor
    )
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    ///     Gets the current caller fingerprint when Spring auth-proof mode is enabled and the request is authenticated.
    /// </summary>
    /// <returns>The derived caller fingerprint, or <see langword="null" /> when no authenticated caller is present.</returns>
    public string? GetFingerprint()
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name;
        return string.IsNullOrWhiteSpace(userId) ? null : $"spring-user:{userId}";
    }
}