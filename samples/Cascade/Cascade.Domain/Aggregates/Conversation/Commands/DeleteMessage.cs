using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.Conversation.Commands;

/// <summary>
///     Command to delete a message in a conversation.
/// </summary>
[GenerateCommand(Route = "delete")]
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.Commands.DeleteMessage")]
internal sealed record DeleteMessage
{
    /// <summary>
    ///     Gets the user ID of the person deleting the message.
    /// </summary>
    [Id(1)]
    public required string DeletedBy { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }
}