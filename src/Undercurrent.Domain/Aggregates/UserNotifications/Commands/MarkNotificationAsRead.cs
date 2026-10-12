using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;

/// <summary>
///     Requests that an existing notification be marked as read.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands.MarkNotificationAsRead")]
internal sealed record MarkNotificationAsRead
{
    /// <summary>
    ///     Gets the exact notification identifier within the user's inbox.
    /// </summary>
    [Id(0)]
    public required string NotificationId { get; init; }
}