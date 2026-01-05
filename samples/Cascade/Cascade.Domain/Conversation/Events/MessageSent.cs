using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Conversation.Events;

/// <summary>
///     Event raised when a message is sent.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "MESSAGESENT", version: 1)]
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.Events.MessageSent")]
internal sealed record MessageSent
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    [Id(1)]
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    [Id(3)]
    public required DateTimeOffset SentAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the sender.
    /// </summary>
    [Id(2)]
    public required string SentBy { get; init; }
}