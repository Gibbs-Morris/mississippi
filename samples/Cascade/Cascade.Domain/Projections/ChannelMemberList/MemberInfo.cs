using System;

using Orleans;


namespace Cascade.Domain.Projections.ChannelMemberList;

/// <summary>
///     Represents a member of a channel.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.ChannelMemberList.MemberInfo")]
internal sealed record MemberInfo
{
    /// <summary>
    ///     Gets the timestamp when the user joined the channel.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset JoinedAt { get; init; }

    /// <summary>
    ///     Gets the user identifier.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}