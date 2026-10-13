using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;

/// <summary>
///     Records when a notification was first marked as read.
/// </summary>
[EventStorageName("MISSISSIPPI", "UNDERCURRENT", "NOTIFICATIONMARKEDASREAD", 1)]
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events.NotificationMarkedAsRead")]
internal sealed record NotificationMarkedAsRead
{
    /// <summary>
    ///     Gets the notification identifier.
    /// </summary>
    [Id(0)]
    public required string NotificationId { get; init; }

    /// <summary>
    ///     Gets the server UTC timestamp of the first read.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset ReadAt { get; init; }
}