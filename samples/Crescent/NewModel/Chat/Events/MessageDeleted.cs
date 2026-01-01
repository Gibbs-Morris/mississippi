using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.NewModel.Chat.Events;

/// <summary>
///     Event raised when a message is deleted from the chat.
/// </summary>
[EventName("CRESCENT", "NEWMODEL", "MESSAGEDELETED")]
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.Events.MessageDeleted")]
internal sealed record MessageDeleted
{
    /// <summary>
    ///     Gets the unique identifier for the deleted message.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }
}