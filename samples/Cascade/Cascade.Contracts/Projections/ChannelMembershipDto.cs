using System;


namespace Cascade.Contracts.Projections;

/// <summary>
///     Represents a user's membership in a channel.
/// </summary>
public sealed record ChannelMembershipDto
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the channel name (denormalized for display).
    /// </summary>
    public string ChannelName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the timestamp when the user joined the channel.
    /// </summary>
    public required DateTimeOffset JoinedAt { get; init; }
}