using System;

using Orleans;


namespace Cascade.Domain.Projections.UserChannelList;

/// <summary>
///     Represents a user's membership in a channel.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.UserChannelList.ChannelMembership")]
internal sealed record ChannelMembership
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the user joined the channel.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset JoinedAt { get; init; }
}