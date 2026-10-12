using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;

/// <summary>
///     Requests creation of a notification in the user's inbox.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands.CreateNotification")]
internal sealed record CreateNotification
{
    /// <summary>
    ///     Gets the optional plain-text description.
    /// </summary>
    [Id(2)]
    public string? Description { get; init; }

    /// <summary>
    ///     Gets the optional symbolic icon identifier.
    /// </summary>
    [Id(3)]
    public string? IconKey { get; init; }

    /// <summary>
    ///     Gets the notification identifier used to recognize retries within this inbox.
    /// </summary>
    [Id(0)]
    public required string NotificationId { get; init; }

    /// <summary>
    ///     Gets the optional navigation target.
    /// </summary>
    [Id(4)]
    public string? TargetUrl { get; init; }

    /// <summary>
    ///     Gets the required notification heading.
    /// </summary>
    [Id(1)]
    public required string Title { get; init; }
}