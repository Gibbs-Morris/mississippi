using Orleans;


namespace Crescent.NewModel.Chat.Commands;

/// <summary>
///     Command to edit an existing message in the chat.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.Commands.EditMessage")]
internal sealed record EditMessage
{
    /// <summary>
    ///     Gets the unique identifier for the message to edit.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the new content for the message.
    /// </summary>
    [Id(1)]
    public required string NewContent { get; init; }
}