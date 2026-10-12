using System;
using System.Collections.Immutable;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;

/// <summary>
///     Holds the notifications for the user identified by the aggregate entity key.
/// </summary>
[BrookName("MISSISSIPPI", "UNDERCURRENT", "USERNOTIFICATIONS")]
[SnapshotStorageName("MISSISSIPPI", "UNDERCURRENT", "USERNOTIFICATIONS", 1)]
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.UserNotificationsAggregate")]
internal sealed record UserNotificationsAggregate
{
    /// <summary>
    ///     Gets the notifications indexed by their exact, case-sensitive identifiers.
    /// </summary>
    [Id(0)]
    public ImmutableDictionary<string, UserNotification> Notifications { get; init; } =
        ImmutableDictionary<string, UserNotification>.Empty.WithComparers(StringComparer.Ordinal);
}