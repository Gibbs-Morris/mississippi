using System;


namespace Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;

/// <summary>Provides capacities, request lifetime, and input limits for per-user notification inboxes.</summary>
/// <remarks>
///     Public so hosts can configure notification policy. Counts bound current state; event history has separate
///     retention.
///     The supported notification cap is 10,000 and the receipt cap is 100,000. Combined input length limits cannot exceed
///     16,384 UTF-16 code units, keeping recorded creation and cleanup events within a conservative size budget.
/// </remarks>
public sealed class UserNotificationsOptions
{
    /// <summary>Gets or sets the maximum UTF-16 length of a creation key. Defaults to 256.</summary>
    public int MaxCreationKeyLength { get; set; } = 256;

    /// <summary>Gets or sets the total receipt capacity. Must exceed notification capacity. Defaults to 10,000.</summary>
    public int MaxCreationReceipts { get; set; } = 10_000;

    /// <summary>Gets or sets the maximum UTF-16 description length. Zero permits only absent or empty descriptions.</summary>
    public int MaxDescriptionLength { get; set; } = 2_000;

    /// <summary>Gets or sets the maximum UTF-16 icon key length. Defaults to 128.</summary>
    public int MaxIconKeyLength { get; set; } = 128;

    /// <summary>Gets or sets the maximum UTF-16 navigation target length. Defaults to 2,048.</summary>
    public int MaxNavigationTargetLength { get; set; } = 2_048;

    /// <summary>Gets or sets the retained notification capacity, including unread entries. Defaults to 1,000.</summary>
    public int MaxNotifications { get; set; } = 1_000;

    /// <summary>Gets or sets the maximum future creation-request lifetime. Defaults to seven days.</summary>
    /// <remarks>Accepted request deadlines remain fixed when this setting changes.</remarks>
    public TimeSpan MaxRequestLifetime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Gets or sets the maximum serialized snapshot payload size in bytes. Defaults to 1,000,000.</summary>
    /// <remarks>
    ///     Creation and first-read commands fail before emitting events when the candidate payload exceeds this limit.
    ///     The default leaves room for base64 encoding and document metadata in Cosmos storage. Hosts raising it must
    ///     verify their snapshot provider's complete document limit. Count capacities remain upper bounds.
    /// </remarks>
    public int MaxSnapshotSizeBytes { get; set; } = 1_000_000;

    /// <summary>Gets or sets the maximum UTF-16 title length. Defaults to 200.</summary>
    public int MaxTitleLength { get; set; } = 200;
}