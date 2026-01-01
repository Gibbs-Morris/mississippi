using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.NewModel.Chat.Events;

/// <summary>
///     Event raised when a chat is created.
/// </summary>
[EventName("CRESCENT", "NEWMODEL", "CHATCREATED")]
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.Events.ChatCreated")]
internal sealed record ChatCreated
{
    /// <summary>
    ///     Gets the name of the chat.
    /// </summary>
    [Id(0)]
    public required string Name { get; init; }
}