using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.Conversation.Commands;

/// <summary>
///     Command to edit a message in a conversation.
/// </summary>
[GenerateCommand(Route = "edit")]
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.Commands.EditMessage")]
internal sealed record EditMessage
{
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