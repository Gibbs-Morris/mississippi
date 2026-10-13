using System;
using System.Collections.Immutable;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;

// Keep the persisted schema version explicit when CleanupCode runs.
// ReSharper disable once RedundantArgumentDefaultValue
/// <summary>
///     Holds the notifications for the user identified by the aggregate entity key.
/// </summary>
/// <remarks>Public state lets callers address the per-user inbox through the generic aggregate API.</remarks>
[SnapshotStorageName("MISSISSIPPI", "UNDERCURRENT", "USERNOTIFICATIONS", 1)]
[BrookName("MISSISSIPPI", "UNDERCURRENT", "USERNOTIFICATIONS")]
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.UserNotificationsAggregate")]
public sealed record UserNotificationsAggregate
{
    /// <summary>Gets compact creation receipts for retained and deleted notifications.</summary>
    [Id(2)]
    public ImmutableDictionary<string, NotificationCreationReceipt> CreationReceipts { get; init; } =
        ImmutableDictionary<string, NotificationCreationReceipt>.Empty.WithComparers(StringComparer.Ordinal);

    /// <summary>Gets the inclusive expiry cutoff already observed during receipt cleanup.</summary>
    /// <remarks>Preserves rejection of forgotten expired requests when a server clock moves backwards.</remarks>
    [Id(3)]
    public DateTimeOffset? ExpiredRequestsThrough { get; init; }

    /// <summary>Gets notification IDs in creation order, oldest first.</summary>
    [Id(1)]
    public ImmutableArray<string> NotificationOrder { get; init; } = ImmutableArray<string>.Empty;

    /// <summary>
    ///     Gets the notifications indexed by their exact, case-sensitive identifiers.
    /// </summary>
    [Id(0)]
    public ImmutableDictionary<string, UserNotification> Notifications { get; init; } =
        ImmutableDictionary<string, UserNotification>.Empty.WithComparers(StringComparer.Ordinal);
}