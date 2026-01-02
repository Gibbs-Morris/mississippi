using Orleans;


namespace Cascade.Domain.Conversation.Commands;

/// <summary>
///     Command to start a new conversation.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.Commands.StartConversation")]
internal sealed record StartConversation
{
    /// <summary>
    ///     Gets the channel identifier for the conversation.
    /// </summary>
    [Id(1)]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the conversation identifier.
    /// </summary>
    [Id(0)]
    public required string ConversationId { get; init; }
}