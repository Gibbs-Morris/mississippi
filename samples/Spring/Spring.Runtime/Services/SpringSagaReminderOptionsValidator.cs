using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.Abstractions;

using Orleans.Timers;


namespace MississippiSamples.Spring.Runtime.Services;

/// <summary>
///     Validates Spring runtime reminder settings before the host starts serving traffic.
/// </summary>
internal sealed class SpringSagaReminderOptionsValidator : IValidateOptions<SagaRecoveryOptions>
{
    private readonly IReminderRegistry[] reminderRegistries;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringSagaReminderOptionsValidator" /> class.
    /// </summary>
    /// <param name="reminderRegistries">The configured Orleans reminder registries visible to the runtime host.</param>
    public SpringSagaReminderOptionsValidator(
        IEnumerable<IReminderRegistry> reminderRegistries
    )
    {
        ArgumentNullException.ThrowIfNull(reminderRegistries);
        this.reminderRegistries = reminderRegistries.ToArray();
    }

    /// <summary>
    ///     Validates the configured saga recovery options.
    /// </summary>
    /// <param name="name">The named options instance being validated.</param>
    /// <param name="options">The configured saga recovery options.</param>
    /// <returns>The validation outcome.</returns>
    public ValidateOptionsResult Validate(
        string? name,
        SagaRecoveryOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        List<string> failures = new();
        if (options.InitialReminderDueTime < TimeSpan.Zero)
        {
            failures.Add("SagaRecovery:InitialReminderDueTime must be zero or positive.");
        }

        if (options.ReminderPeriod <= TimeSpan.Zero)
        {
            failures.Add("SagaRecovery:ReminderPeriod must be greater than zero.");
        }

        if (options.MaxAutomaticAttempts <= 0)
        {
            failures.Add("SagaRecovery:MaxAutomaticAttempts must be greater than zero.");
        }

        if (options.Enabled && !options.ForceManualOnly && (reminderRegistries.Length == 0))
        {
            failures.Add(
                "Automatic saga recovery is enabled but no Orleans reminder provider is configured. Configure Azure Table reminders or force manual-only mode.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}