using Orleans;


namespace Crescent.NewModel.Chat.Commands;

/// <summary>
///     Command to add a message to the chat.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.Commands.AddMessage")]
internal sealed record AddMessage
{
    /// <summary>
    ///     Gets the author of the message.
    /// </summary>
    [Id(2)]
    public required string Author { get; init; }

    /// <summary>
    ///     Gets the content of the message.
    /// </summary>
    [Id(1)]
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the unique identifier for the message.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }
}