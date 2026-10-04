using System;
using System.Threading.Tasks;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Provides an internal seam for Orleans reminder operations used by saga aggregates.
/// </summary>
internal interface ISagaReminderRegistry
{
    /// <summary>
    ///     Gets the reminder with the specified name for the provided grain.
    /// </summary>
    /// <param name="grain">The grain instance.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <returns>The matching reminder when found; otherwise, <see langword="null" />.</returns>
    Task<IGrainReminder?> GetReminderAsync(
        IGrainBase grain,
        string reminderName
    );

    /// <summary>
    ///     Registers or updates the reminder with the specified schedule for the provided grain.
    /// </summary>
    /// <param name="grain">The grain instance.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <param name="dueTime">The due time before the first tick.</param>
    /// <param name="period">The recurring period.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterOrUpdateAsync(
        IGrainBase grain,
        string reminderName,
        TimeSpan dueTime,
        TimeSpan period
    );

    /// <summary>
    ///     Unregisters the specified reminder for the provided grain.
    /// </summary>
    /// <param name="grain">The grain instance.</param>
    /// <param name="reminder">The reminder to unregister.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnregisterAsync(
        IGrainBase grain,
        IGrainReminder reminder
    );
}