using System;

using Mississippi.Inlet.Projection.Abstractions;


namespace Cascade.Contracts.Projections;

/// <summary>
///     Client DTO for an individual channel summary projection.
/// </summary>
/// <remarks>
///     This represents a single channel's metadata. Clients subscribe to individual
///     channel summaries based on IDs from <see cref="AllChannelIdsDto" />,
///     enabling efficient viewport-based rendering.
/// </remarks>
[ProjectionPath("cascade/channel-summaries")]
public sealed record ChannelSummaryDto
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the channel was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the channel creator.
    /// </summary>
    public required string CreatedBy { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the channel is archived.
    /// </summary>
    public bool IsArchived { get; init; }

    /// <summary>
    ///     Gets the number of members in the channel.
    /// </summary>
    public int MemberCount { get; init; }

    /// <summary>
    ///     Gets the channel name.
    /// </summary>
    public required string Name { get; init; }
}