namespace Cascade.Contracts.Api;

/// <summary>
///     Request to send a message in a conversation.
/// </summary>
public sealed record SendMessageRequest
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the user ID of the sender.
    /// </summary>
    public required string SentBy { get; init; }
}