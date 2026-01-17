using System.Collections.Generic;

using Mississippi.Inlet.Projection.Abstractions;


namespace Cascade.Contracts.Projections;

/// <summary>
///     Client DTO for the channel message IDs projection.
/// </summary>
/// <remarks>
///     This lightweight projection contains only message IDs for a channel,
///     enabling efficient viewport-based subscriptions. Clients subscribe to
///     this to get the message list, then subscribe to individual
///     <see cref="MessageDto" /> projections for messages in view.
/// </remarks>
[ProjectionPath("cascade/channel-message-ids")]
public sealed record ChannelMessageIdsDto
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the ordered list of message IDs (newest last).
    /// </summary>
    public required IReadOnlyList<string> MessageIds { get; init; }

    /// <summary>
    ///     Gets the total count of messages.
    /// </summary>
    public required int TotalCount { get; init; }
}