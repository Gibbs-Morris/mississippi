using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.NewModel.Chat.Events;

/// <summary>
///     Event raised when a message is added to the chat.
/// </summary>
[EventName("CRESCENT", "NEWMODEL", "MESSAGEADDED")]
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.Events.MessageAdded")]
internal sealed record MessageAdded
{
    /// <summary>
    ///     Gets the unique identifier for the message.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the content of the message.
    /// </summary>
    [Id(1)]
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the author of the message.
    /// </summary>
    [Id(2)]
    public required string Author { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was created.
    /// </summary>
    [Id(3)]
    public DateTimeOffset CreatedAt { get; init; }
}
