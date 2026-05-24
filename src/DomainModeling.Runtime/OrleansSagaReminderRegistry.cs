using System;
using System.Threading.Tasks;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Uses Orleans reminder extension methods to manage saga reminders for POCO grains.
/// </summary>
internal sealed class OrleansSagaReminderRegistry : ISagaReminderRegistry
{
    /// <inheritdoc />
    public Task<IGrainReminder?> GetReminderAsync(
        IGrainBase grain,
        string reminderName
    )
    {
        ArgumentNullException.ThrowIfNull(grain);
        ArgumentException.ThrowIfNullOrWhiteSpace(reminderName);
        return grain.GetReminder(reminderName);
    }

    /// <inheritdoc />
    public async Task RegisterOrUpdateAsync(
        IGrainBase grain,
        string reminderName,
        TimeSpan dueTime,
        TimeSpan period
    )
    {
        ArgumentNullException.ThrowIfNull(grain);
        ArgumentException.ThrowIfNullOrWhiteSpace(reminderName);
        _ = await grain.RegisterOrUpdateReminder(reminderName, dueTime, period);
    }

    /// <inheritdoc />
    public Task UnregisterAsync(
        IGrainBase grain,
        IGrainReminder reminder
    )
    {
        ArgumentNullException.ThrowIfNull(grain);
        ArgumentNullException.ThrowIfNull(reminder);
        return grain.UnregisterReminder(reminder);
    }
}