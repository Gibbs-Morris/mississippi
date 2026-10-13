using System;

using Orleans;


namespace Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;

/// <summary>
///     Requests creation of a notification in the user's inbox.
/// </summary>
/// <remarks>Public requests let producers submit stable creation keys and deadlines directly to the aggregate.</remarks>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands.CreateNotification")]
public sealed record CreateNotification
{
    /// <summary>
    ///     Gets the stable, case-sensitive creation key reused with unchanged content and expiry on retries.
    /// </summary>
    [Id(0)]
    public required string CreationKey { get; init; }

    /// <summary>
    ///     Gets the optional plain-text description.
    /// </summary>
    [Id(2)]
    public string? Description { get; init; }

    /// <summary>Gets the fixed request deadline, supplied once and preserved on every retry.</summary>
    /// <remarks>This expires the creation request; it does not expire the notification.</remarks>
    [Id(5)]
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    ///     Gets the optional symbolic icon identifier.
    /// </summary>
    [Id(3)]
    public string? IconKey { get; init; }

    /// <summary>
    ///     Gets the optional navigation target.
    /// </summary>
    [Id(4)]
    public string? NavigationTarget { get; init; }

    /// <summary>
    ///     Gets the required notification heading.
    /// </summary>
    [Id(1)]
    public required string Title { get; init; }
}