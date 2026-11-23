using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Undercurrent.Abstractions.Projections.UserNotificationCounts;

// Keep the persisted schema version explicit when CleanupCode runs.
// ReSharper disable once RedundantArgumentDefaultValue
/// <summary>Provides total and unread counts for the user's currently retained notifications.</summary>
/// <remarks>Public so applications can read counters through the existing generic projection API.</remarks>
[BrookName("MISSISSIPPI", "UNDERCURRENT", "USERNOTIFICATIONS")]
[SnapshotStorageName("MISSISSIPPI", "UNDERCURRENT", "USERNOTIFICATIONCOUNTS", 1)]
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Abstractions.Projections.UserNotificationCounts.UserNotificationCountsProjection")]
public sealed record UserNotificationCountsProjection
{
    /// <summary>Gets the number of retained notifications, including read entries.</summary>
    [Id(0)]
    public int TotalCount { get; init; }

    /// <summary>Gets the number of retained notifications without a read timestamp.</summary>
    [Id(1)]
    public int UnreadCount { get; init; }

    /// <summary>Gets serialized ID/read-status bookkeeping for ID-only removal and read facts.</summary>
    [Id(2)]
    [JsonInclude]
    internal ImmutableDictionary<string, bool> ReadStates { get; init; } =
        ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal);
}