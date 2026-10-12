using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;

/// <summary>
///     Requests deletion of a notification from the user's inbox.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands.DeleteNotification")]
internal sealed record DeleteNotification
{
    /// <summary>
    ///     Gets the exact notification identifier to delete.
    /// </summary>
    [Id(0)]
    public required string NotificationId { get; init; }
}