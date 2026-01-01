using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.NewModel.Chat.Events;

/// <summary>
///     Event raised when a message is edited in the chat.
/// </summary>
[EventName("CRESCENT", "NEWMODEL", "MESSAGEEDITED")]
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.Events.MessageEdited")]
internal sealed record MessageEdited
{
    /// <summary>
    ///     Gets the unique identifier for the message.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the previous content of the message.
    /// </summary>
    [Id(1)]
    public required string PreviousContent { get; init; }

    /// <summary>
    ///     Gets the new content of the message.
    /// </summary>
    [Id(2)]
    public required string NewContent { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was edited.
    /// </summary>
    [Id(3)]
    public DateTimeOffset EditedAt { get; init; }
}
