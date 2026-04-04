using System;
using System.Threading.Tasks;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Provides testable access to Orleans reminder registration operations for POCO grains.
/// </summary>
internal interface IGrainReminderManager
{
    /// <summary>
    ///     Gets an existing reminder by name for the active grain, when present.
    /// </summary>
    /// <param name="grain">The active grain instance.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <returns>The matching reminder, or <c>null</c> when none is registered.</returns>
    Task<IGrainReminder?> GetReminderAsync(
        IGrainBase grain,
        string reminderName
    );

    /// <summary>
    ///     Registers or updates a reminder for the active grain.
    /// </summary>
    /// <param name="grain">The active grain instance.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <param name="dueTime">The initial due time.</param>
    /// <param name="period">The reminder period.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterOrUpdateReminderAsync(
        IGrainBase grain,
        string reminderName,
        TimeSpan dueTime,
        TimeSpan period
    );

    /// <summary>
    ///     Unregisters a reminder for the active grain.
    /// </summary>
    /// <param name="grain">The active grain instance.</param>
    /// <param name="reminder">The reminder to unregister.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnregisterReminderAsync(
        IGrainBase grain,
        IGrainReminder reminder
    );
}