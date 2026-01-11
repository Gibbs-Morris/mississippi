using System.Collections.Generic;
using System.Text.Json.Serialization;

using Mississippi.Inlet.Blazor.WebAssembly.Effects;


namespace Cascade.Web.Contracts;

/// <summary>
///     Response containing messages for a conversation/channel.
/// </summary>
/// <remarks>
///     This contract mirrors <c>ChannelMessagesProjection</c> from the server
///     and can be deserialized directly from the projection JSON response.
///     The <see cref="UxProjectionDtoAttribute" /> links this DTO to the server
///     projection via the route string.
/// </remarks>
[UxProjectionDto("channel-messages")]
public sealed record ConversationMessagesResponse
{
    /// <summary>
    ///     Gets the channel/conversation identifier.
    /// </summary>
    /// <remarks>
    ///     Maps to <c>ChannelId</c> in the server projection.
    /// </remarks>
    [JsonPropertyName("channelId")]
    public required string ConversationId { get; init; }

    /// <summary>
    ///     Gets the total count of messages.
    /// </summary>
    public required int MessageCount { get; init; }

    /// <summary>
    ///     Gets the list of messages.
    /// </summary>
    public required IReadOnlyList<ConversationMessageItem> Messages { get; init; }
}