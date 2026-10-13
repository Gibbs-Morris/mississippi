using Orleans;


namespace Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;

/// <summary>
///     Requests that an existing notification be marked as read.
/// </summary>
/// <remarks>Public requests let callers acknowledge a notification through the aggregate command API.</remarks>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands.MarkNotificationAsRead")]
public sealed record MarkNotificationAsRead
{
    /// <summary>
    ///     Gets the exact notification identifier within the user's inbox.
    /// </summary>
    [Id(0)]
    public required string NotificationId { get; init; }
}