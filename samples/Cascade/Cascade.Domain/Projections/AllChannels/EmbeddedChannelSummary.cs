using System;

using Orleans;


namespace Cascade.Domain.Projections.AllChannels;

/// <summary>
///     Represents a summary of a channel embedded in the AllChannelsProjection.
/// </summary>
/// <remarks>
///     This type is DEPRECATED. The new pattern uses AllChannelIdsProjection + ChannelSummaryProjection
///     for efficient viewport-based subscriptions. This type remains for backwards compatibility.
/// </remarks>
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.AllChannels.ChannelSummary")]
public sealed record EmbeddedChannelSummary
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the channel was created.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the channel creator.
    /// </summary>
    [Id(3)]
    public required string CreatedBy { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the channel is archived.
    /// </summary>
    [Id(4)]
    public bool IsArchived { get; init; }

    /// <summary>
    ///     Gets the number of members in the channel.
    /// </summary>
    [Id(5)]
    public int MemberCount { get; init; }

    /// <summary>
    ///     Gets the channel name.
    /// </summary>
    [Id(1)]
    public required string Name { get; init; }
}