using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;

/// <summary>
///     Records deletion of one notification from the inbox.
/// </summary>
[EventStorageName("MISSISSIPPI", "UNDERCURRENT", "NOTIFICATIONDELETED", 1)]
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events.NotificationDeleted")]
internal sealed record NotificationDeleted
{
    /// <summary>
    ///     Gets the identifier of the deleted notification.
    /// </summary>
    [Id(0)]
    public required string NotificationId { get; init; }
}