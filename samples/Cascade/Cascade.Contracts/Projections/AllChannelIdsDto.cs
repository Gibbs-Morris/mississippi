using System.Collections.Generic;

using Mississippi.Inlet.Projection.Abstractions;


namespace Cascade.Contracts.Projections;

/// <summary>
///     Client DTO for the all-channel-IDs projection.
/// </summary>
/// <remarks>
///     This lightweight projection contains only channel IDs, enabling
///     efficient viewport-based subscriptions. Clients subscribe to this
///     to discover available channels, then subscribe to individual
///     <see cref="ChannelSummaryDto" /> projections for details.
/// </remarks>
[ProjectionPath("cascade/channel-ids")]
public sealed record AllChannelIdsDto
{
    /// <summary>
    ///     Gets the set of all active channel IDs.
    /// </summary>
    public required IReadOnlySet<string> ChannelIds { get; init; }

    /// <summary>
    ///     Gets the total count of channels.
    /// </summary>
    public required int TotalCount { get; init; }
}