using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;


namespace MississippiSamples.Spring.Gateway;

/// <summary>
///     Reports whether Spring auth-proof mode has the saga-access propagation dependencies required by the gateway.
/// </summary>
internal sealed class SpringSagaAuthorizationHealthCheck : IHealthCheck
{
    private IOptionsMonitor<SpringAuthOptions> SpringAuthOptions { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringSagaAuthorizationHealthCheck" /> class.
    /// </summary>
    /// <param name="springAuthOptions">The current Spring auth-proof configuration.</param>
    /// <param name="accessContextProvider">Provider used to derive saga access fingerprints from authenticated callers.</param>
    public SpringSagaAuthorizationHealthCheck(
        IOptionsMonitor<SpringAuthOptions> springAuthOptions,
        SpringSagaAccessContextProvider accessContextProvider
    )
    {
        ArgumentNullException.ThrowIfNull(springAuthOptions);
        ArgumentNullException.ThrowIfNull(accessContextProvider);
        SpringAuthOptions = springAuthOptions;
    }

    /// <summary>
    ///     Checks gateway readiness for saga-access propagation.
    /// </summary>
    /// <param name="context">The health-check context.</param>
    /// <param name="cancellationToken">A token that cancels the health check.</param>
    /// <returns>The health-check result.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!SpringAuthOptions.CurrentValue.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Spring auth-proof mode is disabled."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Spring saga access fingerprint propagation is configured."));
    }
}