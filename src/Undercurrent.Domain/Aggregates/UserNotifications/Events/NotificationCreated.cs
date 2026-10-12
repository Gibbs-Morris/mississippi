using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;

/// <summary>
///     Records the content and creation timestamp of a notification added to an inbox.
/// </summary>
[EventStorageName("MISSISSIPPI", "UNDERCURRENT", "NOTIFICATIONCREATED", 1)]
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events.NotificationCreated")]
internal sealed record NotificationCreated
{
    /// <summary>
    ///     Gets the notification that was created.
    /// </summary>
    [Id(0)]
    public required UserNotification Notification { get; init; }
}