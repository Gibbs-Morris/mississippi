using System;
using System.Collections.Immutable;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;

using Orleans;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;

/// <summary>Records one creation together with its capacity removals and receipt-expiry cutoff.</summary>
[EventStorageName("MISSISSIPPI", "UNDERCURRENT", "NOTIFICATIONCREATED", 1)]
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events.NotificationCreated")]
internal sealed record NotificationCreated
{
    /// <summary>Gets the stable creation key within this inbox.</summary>
    [Id(1)]
    public required string CreationKey { get; init; }

    /// <summary>Gets the notification IDs selected for capacity eviction.</summary>
    [Id(4)]
    public ImmutableArray<string> EvictedNotificationIds { get; init; } = ImmutableArray<string>.Empty;

    /// <summary>Gets the inclusive cutoff for pruning expired receipts after content removal.</summary>
    [Id(5)]
    public required DateTimeOffset ExpiredRequestsThrough { get; init; }

    /// <summary>Gets the original fixed UTC request expiry.</summary>
    [Id(2)]
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Gets the notification assigned by the handler.</summary>
    [Id(0)]
    public required UserNotification Notification { get; init; }

    /// <summary>Gets the versioned request fingerprint.</summary>
    [Id(3)]
    public required string RequestFingerprint { get; init; }
}