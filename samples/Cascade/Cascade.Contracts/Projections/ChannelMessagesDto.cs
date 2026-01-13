using System.Collections.Generic;
using System.Text.Json.Serialization;

using Mississippi.Inlet.Projection.Abstractions;


namespace Cascade.Contracts.Projections;

/// <summary>
///     Client DTO for channel message history projection.
/// </summary>
/// <remarks>
///     This contract mirrors <c>ChannelMessagesProjection</c> from the server
///     and can be deserialized directly from the projection JSON response.
///     The <see cref="ProjectionPathAttribute" /> links this DTO to the server
///     projection via the shared path.
/// </remarks>
[ProjectionPath("cascade/channels")]
public sealed record ChannelMessagesDto
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [JsonPropertyName("channelId")]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the total count of messages.
    /// </summary>
    public required int MessageCount { get; init; }

    /// <summary>
    ///     Gets the list of messages.
    /// </summary>
    public required IReadOnlyList<ChannelMessageItem> Messages { get; init; }
}