using System;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Configures framework-owned saga reminder recovery behavior.
/// </summary>
/// <remarks>
///     Justification: public so hosts can configure saga reminder recovery through the options pattern.
/// </remarks>
public sealed class SagaRecoveryOptions
{
    /// <summary>
    ///     Gets a value indicating whether automatic saga recovery reminders are enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    ///     Gets a value indicating whether runtime configuration should force all sagas into manual-only recovery.
    /// </summary>
    public bool ForceManualOnly { get; init; }

    /// <summary>
    ///     Gets the delay used when a saga is eligible for automatic recovery and no explicit next-eligible timestamp exists.
    /// </summary>
    public TimeSpan InitialReminderDueTime { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Gets the maximum number of reminder-driven automatic resume attempts before reminders are disarmed.
    /// </summary>
    public int MaxAutomaticAttempts { get; init; } = 10;

    /// <summary>
    ///     Gets the Orleans reminder period used for registered saga recovery reminders.
    /// </summary>
    public TimeSpan ReminderPeriod { get; init; } = TimeSpan.FromMinutes(5);
}