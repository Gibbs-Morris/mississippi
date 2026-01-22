using System;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.Conversation;

/// <summary>
///     Represents the state of a conversation aggregate.
/// </summary>
[BrookName("CASCADE", "CHAT", "CONVERSATION")]
[SnapshotStorageName("CASCADE", "CHAT", "CONVERSATIONSTATE")]
[GenerateAggregateEndpoints]
[GenerateSerializer]
[Alias("Cascade.Domain.Conversation.ConversationAggregate")]
internal sealed record ConversationAggregate
{
    /// <summary>
    ///     Gets the channel identifier this conversation belongs to.
    /// </summary>
    [Id(2)]
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the conversation identifier.
    /// </summary>
    [Id(1)]
    public string ConversationId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the conversation has been started.
    /// </summary>
    [Id(0)]
    public bool IsStarted { get; init; }

    /// <summary>
    ///     Gets the messages in the conversation.
    /// </summary>
    [Id(4)]
    public ImmutableList<Message> Messages { get; init; } = ImmutableList<Message>.Empty;

    /// <summary>
    ///     Gets the timestamp when the conversation was started.
    /// </summary>
    [Id(3)]
    public DateTimeOffset StartedAt { get; init; }
}