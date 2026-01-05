using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Conversation.Events;

/// <summary>
///     Event raised when a conversation is started.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "CONVERSATIONSTARTED", version: 1)]
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.Events.ConversationStarted")]
internal sealed record ConversationStarted
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(1)]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the conversation identifier.
    /// </summary>
    [Id(0)]
    public required string ConversationId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the conversation was started.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset StartedAt { get; init; }
}