using System;

using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;

/// <summary>
///     Represents a notification stored in a user's inbox.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.UserNotification")]
internal sealed record UserNotification
{
    /// <summary>
    ///     Gets the server-assigned UTC creation timestamp.
    /// </summary>
    [Id(5)]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///     Gets the optional plain-text description.
    /// </summary>
    [Id(2)]
    public string? Description { get; init; }

    /// <summary>
    ///     Gets the optional symbolic icon identifier interpreted by the consuming application.
    /// </summary>
    [Id(3)]
    public string? IconKey { get; init; }

    /// <summary>
    ///     Gets the caller-supplied notification identifier within this user's inbox.
    /// </summary>
    [Id(0)]
    public required string NotificationId { get; init; }

    /// <summary>
    ///     Gets the first server UTC timestamp when the notification was marked as read.
    /// </summary>
    [Id(6)]
    public DateTimeOffset? ReadAt { get; init; }

    /// <summary>
    ///     Gets the optional navigation target interpreted by the consuming application.
    /// </summary>
    [Id(4)]
    public string? TargetUrl { get; init; }

    /// <summary>
    ///     Gets the notification heading.
    /// </summary>
    [Id(1)]
    public required string Title { get; init; }
}