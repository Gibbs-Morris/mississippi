using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;

/// <summary>
///     Requests deletion of notifications created more than the specified number of days ago.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands.DeleteNotificationsOlderThan")]
internal sealed record DeleteNotificationsOlderThan
{
    /// <summary>
    ///     Gets the nonnegative age in days; notifications exactly at the cutoff remain.
    /// </summary>
    [Id(0)]
    public required int Days { get; init; }
}