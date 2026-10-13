using System.Collections.Immutable;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;

using Orleans;


namespace Mississippi.Undercurrent.Abstractions.Projections.UserNotificationList;

// Keep the persisted schema version explicit when CleanupCode runs.
// ReSharper disable once RedundantArgumentDefaultValue
/// <summary>Provides the retained notification content in newest creation order for a future user interface.</summary>
/// <remarks>Public so applications can read presentation data through the existing generic projection API.</remarks>
[BrookName("MISSISSIPPI", "UNDERCURRENT", "USERNOTIFICATIONS")]
[SnapshotStorageName("MISSISSIPPI", "UNDERCURRENT", "USERNOTIFICATIONLIST", 1)]
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Abstractions.Projections.UserNotificationList.UserNotificationListProjection")]
public sealed record UserNotificationListProjection
{
    /// <summary>Gets notification content ordered by creation events, newest first, including read entries.</summary>
    [Id(0)]
    public ImmutableArray<UserNotification> Notifications { get; init; } = [];
}