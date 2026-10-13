using Orleans;


namespace Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;

/// <summary>
///     Requests deletion of a notification from the user's inbox.
/// </summary>
/// <remarks>Public requests let callers remove a generated notification identity from its inbox.</remarks>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands.DeleteNotification")]
public sealed record DeleteNotification
{
    /// <summary>
    ///     Gets the exact notification identifier to delete.
    /// </summary>
    [Id(0)]
    public required string NotificationId { get; init; }
}