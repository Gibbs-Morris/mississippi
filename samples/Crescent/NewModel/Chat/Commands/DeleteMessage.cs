using Orleans;


namespace Crescent.NewModel.Chat.Commands;

/// <summary>
///     Command to delete a message from the chat.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.Commands.DeleteMessage")]
internal sealed record DeleteMessage
{
    /// <summary>
    ///     Gets the unique identifier for the message to delete.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }
}