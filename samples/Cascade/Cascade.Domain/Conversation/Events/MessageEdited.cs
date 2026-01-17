using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Conversation.Events;

/// <summary>
///     Event raised when a message is edited.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "MESSAGEEDITED")]
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.Events.MessageEdited")]
internal sealed record MessageEdited
{
    /// <summary>
    ///     Gets the timestamp when the message was edited.
    /// </summary>
    [Id(3)]
    public required DateTimeOffset EditedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the editor.
    /// </summary>
    [Id(2)]
    public required string EditedBy { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the new message content.
    /// </summary>
    [Id(1)]
    public required string NewContent { get; init; }
}