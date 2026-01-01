using Orleans;


namespace Crescent.NewModel.Chat.Commands;

/// <summary>
///     Command to create a new chat.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.Commands.CreateChat")]
internal sealed record CreateChat
{
    /// <summary>
    ///     Gets the name for the chat.
    /// </summary>
    [Id(0)]
    public required string Name { get; init; }
}
