using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.Abstractions;

using Orleans.Timers;


namespace MississippiSamples.Spring.Runtime.Services;

/// <summary>
///     Reports whether the Spring runtime has the reminder infrastructure required for automatic saga recovery.
/// </summary>
internal sealed class SpringSagaReminderHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<SagaRecoveryOptions> recoveryOptions;

    private readonly IReminderRegistry[] reminderRegistries;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringSagaReminderHealthCheck" /> class.
    /// </summary>
    /// <param name="recoveryOptions">The current saga recovery options.</param>
    /// <param name="reminderRegistries">The configured Orleans reminder registries visible to the runtime host.</param>
    public SpringSagaReminderHealthCheck(
        IOptionsMonitor<SagaRecoveryOptions> recoveryOptions,
        IEnumerable<IReminderRegistry> reminderRegistries
    )
    {
        ArgumentNullException.ThrowIfNull(recoveryOptions);
        ArgumentNullException.ThrowIfNull(reminderRegistries);
        this.recoveryOptions = recoveryOptions;
        this.reminderRegistries = reminderRegistries.ToArray();
    }

    /// <summary>
    ///     Checks reminder-readiness for automatic saga recovery.
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
        SagaRecoveryOptions options = recoveryOptions.CurrentValue;
        if (!options.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Automatic saga recovery is disabled."));
        }

        if (options.ForceManualOnly)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("Automatic saga recovery is forced into manual-only mode."));
        }

        return Task.FromResult(
            reminderRegistries.Length == 0
                ? HealthCheckResult.Unhealthy(
                    "Automatic saga recovery is enabled but the Orleans reminder provider is unavailable.")
                : HealthCheckResult.Healthy("Saga reminder provider is configured."));
    }
}