using System;

using Orleans;


namespace Cascade.Domain.Conversation;

/// <summary>
///     Represents a message in a conversation.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.Message")]
internal sealed record Message
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    [Id(1)]
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was last edited, if any.
    /// </summary>
    [Id(4)]
    public DateTimeOffset? EditedAt { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message has been deleted.
    /// </summary>
    [Id(3)]
    public bool IsDeleted { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    [Id(5)]
    public required DateTimeOffset SentAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the sender.
    /// </summary>
    [Id(2)]
    public required string SentBy { get; init; }
}