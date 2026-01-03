// Copyright (c) Gibbs-Morris. All rights reserved.

using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Projections.ChannelMessages;

/// <summary>
///     Read-optimized projection of message history for a channel or conversation.
/// </summary>
/// <remarks>
///     <para>
///         This projection provides a denormalized view of messages
///         optimized for display in the chat message view UI.
///     </para>
///     <para>
///         Subscribes to events from the Conversation aggregate:
///         ConversationStarted, MessageSent, MessageEdited, MessageDeleted.
///     </para>
/// </remarks>
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELMESSAGES")]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.ChannelMessages.ChannelMessagesProjection")]
internal sealed record ChannelMessagesProjection
{
    /// <summary>
    ///     Gets the channel or conversation identifier.
    /// </summary>
    [Id(0)]
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the total count of messages.
    /// </summary>
    [Id(2)]
    public int MessageCount { get; init; }

    /// <summary>
    ///     Gets the list of messages in the channel.
    /// </summary>
    [Id(1)]
    public ImmutableList<MessageItem> Messages { get; init; } = ImmutableList<MessageItem>.Empty;
}