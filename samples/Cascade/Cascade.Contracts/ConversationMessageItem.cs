using System;


namespace Cascade.Contracts;

/// <summary>
///     Represents a message in a conversation.
/// </summary>
public sealed record ConversationMessageItem
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was edited, if applicable.
    /// </summary>
    public DateTimeOffset? EditedAt { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message has been deleted.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    public required DateTimeOffset SentAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the sender.
    /// </summary>
    public required string SentBy { get; init; }
}