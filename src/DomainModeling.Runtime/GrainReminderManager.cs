using System;
using System.Linq;
using System.Threading.Tasks;

using Orleans;
using Orleans.Runtime;
using Orleans.Timers;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Adapts Orleans reminder registry APIs for POCO aggregate grains.
/// </summary>
internal sealed class GrainReminderManager : IGrainReminderManager
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GrainReminderManager" /> class.
    /// </summary>
    /// <param name="reminderRegistry">The Orleans reminder registry.</param>
    public GrainReminderManager(
        IReminderRegistry reminderRegistry
    )
    {
        ReminderRegistry = reminderRegistry ?? throw new ArgumentNullException(nameof(reminderRegistry));
    }

    private IReminderRegistry ReminderRegistry { get; }

    /// <inheritdoc />
    public async Task<IGrainReminder?> GetReminderAsync(
        IGrainBase grain,
        string reminderName
    )
    {
        ArgumentNullException.ThrowIfNull(grain);
        ArgumentException.ThrowIfNullOrWhiteSpace(reminderName);
        return (await ReminderRegistry.GetReminders(grain.GrainContext.GrainId))
            .SingleOrDefault(reminder => string.Equals(reminder.ReminderName, reminderName, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public Task RegisterOrUpdateReminderAsync(
        IGrainBase grain,
        string reminderName,
        TimeSpan dueTime,
        TimeSpan period
    )
    {
        ArgumentNullException.ThrowIfNull(grain);
        ArgumentException.ThrowIfNullOrWhiteSpace(reminderName);
        return ReminderRegistry.RegisterOrUpdateReminder(grain.GrainContext.GrainId, reminderName, dueTime, period);
    }

    /// <inheritdoc />
    public Task UnregisterReminderAsync(
        IGrainBase grain,
        IGrainReminder reminder
    )
    {
        ArgumentNullException.ThrowIfNull(grain);
        ArgumentNullException.ThrowIfNull(reminder);
        return ReminderRegistry.UnregisterReminder(grain.GrainContext.GrainId, reminder);
    }
}