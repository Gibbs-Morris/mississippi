using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.Conversation.Commands;

/// <summary>
///     Command to send a message in a conversation.
/// </summary>
[GenerateCommand(Route = "send")]
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.Commands.SendMessage")]
internal sealed record SendMessage
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    [Id(1)]
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the user ID of the sender.
    /// </summary>
    [Id(2)]
    public required string SentBy { get; init; }
}