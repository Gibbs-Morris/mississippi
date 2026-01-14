using System.Collections.Generic;

using Mississippi.Inlet.Projection.Abstractions;


namespace Cascade.Contracts.Projections;

/// <summary>
///     Client DTO for the all-channels discovery projection.
/// </summary>
/// <remarks>
///     This contract mirrors <c>AllChannelsProjection</c> from the server
///     and can be deserialized directly from the projection JSON response.
///     The <see cref="ProjectionPathAttribute" /> links this DTO to the server
///     projection via the shared path.
/// </remarks>
[ProjectionPath("cascade/discovery")]
public sealed record AllChannelsDto
{
    /// <summary>
    ///     Gets the dictionary of all available channels keyed by channel ID.
    /// </summary>
    public required IReadOnlyDictionary<string, ChannelSummaryDto> Channels { get; init; }
}