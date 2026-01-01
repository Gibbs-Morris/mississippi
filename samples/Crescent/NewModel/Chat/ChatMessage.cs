using System;

using Orleans;


namespace Crescent.NewModel.Chat;

/// <summary>
///     Represents a single chat message.
/// </summary>
[GenerateSerializer]
[Alias("Crescent.NewModel.Chat.ChatMessage")]
internal sealed record ChatMessage
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
    ///     Gets the timestamp when the message was created.
    /// </summary>
    [Id(3)]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was last edited, if any.
    /// </summary>
    [Id(4)]
    public DateTimeOffset? EditedAt { get; init; }

    /// <summary>
    ///     Gets the unique identifier for the message.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }
}