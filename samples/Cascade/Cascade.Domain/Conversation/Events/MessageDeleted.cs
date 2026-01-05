using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Conversation.Events;

/// <summary>
///     Event raised when a message is deleted.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "MESSAGEDELETED", version: 1)]
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.Events.MessageDeleted")]
internal sealed record MessageDeleted
{
    /// <summary>
    ///     Gets the timestamp when the message was deleted.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset DeletedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the person who deleted the message.
    /// </summary>
    [Id(1)]
    public required string DeletedBy { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    [Id(0)]
    public required string MessageId { get; init; }
}