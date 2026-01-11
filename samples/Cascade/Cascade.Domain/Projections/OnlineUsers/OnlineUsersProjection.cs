using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Projections.OnlineUsers;

/// <summary>
///     Read-optimized projection of currently online users for UX display.
/// </summary>
/// <remarks>
///     <para>
///         This projection provides a denormalized view of online users
///         optimized for display in UI components like presence indicators.
///     </para>
///     <para>
///         Subscribes to events from the User aggregate:
///         UserRegistered, UserWentOnline, UserWentOffline.
///     </para>
///     <para>
///         Note: This projection is maintained per-user (keyed by UserId), tracking
///         the online status of that specific user. For a global view, query
///         multiple user projections.
///     </para>
/// </remarks>
[BrookName("CASCADE", "CHAT", "USER")]
[SnapshotStorageName("CASCADE", "CHAT", "ONLINEUSERS")]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.OnlineUsers.OnlineUsersProjection")]
internal sealed record OnlineUsersProjection
{
    /// <summary>
    ///     Gets the user's display name.
    /// </summary>
    [Id(1)]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the user is currently online.
    /// </summary>
    [Id(2)]
    public bool IsOnline { get; init; }

    /// <summary>
    ///     Gets the timestamp when the user last changed their online status.
    /// </summary>
    [Id(3)]
    public DateTimeOffset? LastStatusChange { get; init; }

    /// <summary>
    ///     Gets the user identifier.
    /// </summary>
    [Id(0)]
    public string UserId { get; init; } = string.Empty;
}