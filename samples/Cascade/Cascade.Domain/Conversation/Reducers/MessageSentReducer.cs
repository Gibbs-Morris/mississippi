// <copyright file="MessageSentReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Conversation.Reducers;

/// <summary>
///     Reduces the <see cref="MessageSent" /> event to produce a new <see cref="ConversationState" />.
/// </summary>
internal sealed class MessageSentReducer : Reducer<MessageSent, ConversationState>
{
    /// <inheritdoc />
    protected override ConversationState ReduceCore(
        ConversationState state,
        MessageSent eventData
    ) =>
        state with
        {
            Messages = state.Messages.Add(
                new()
                {
                    MessageId = eventData.MessageId,
                    Content = eventData.Content,
                    SentBy = eventData.SentBy,
                    SentAt = eventData.SentAt,
                }),
        };
}