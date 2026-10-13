using System;

using Orleans;


namespace Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;

/// <summary>Describes a creation request retained for duplicate detection after content deletion.</summary>
/// <remarks>Public because it is part of the directly readable aggregate state contract.</remarks>
[GenerateSerializer]
[Alias("Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.NotificationCreationReceipt")]
public sealed record NotificationCreationReceipt
{
    /// <summary>Gets the fixed UTC request expiry.</summary>
    [Id(1)]
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Gets the generated notification identity originally assigned to this request.</summary>
    [Id(0)]
    public required string NotificationId { get; init; }

    /// <summary>Gets the versioned digest of the original content and request expiry.</summary>
    [Id(2)]
    public required string RequestFingerprint { get; init; }
}