// <copyright file="MessageEditedReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Conversation.Reducers;

/// <summary>
///     Reduces the <see cref="MessageEdited" /> event to produce a new <see cref="ConversationState" />.
/// </summary>
internal sealed class MessageEditedReducer : Reducer<MessageEdited, ConversationState>
{
    /// <inheritdoc />
    protected override ConversationState ReduceCore(
        ConversationState state,
        MessageEdited eventData
    )
    {
        int index = state.Messages.FindIndex(m => m.MessageId == eventData.MessageId);
        if (index < 0)
        {
            return state with
            {
            };
        }

        Message existingMessage = state.Messages[index];
        Message updatedMessage = existingMessage with
        {
            Content = eventData.NewContent,
            EditedAt = eventData.EditedAt,
        };
        return state with
        {
            Messages = state.Messages.SetItem(index, updatedMessage),
        };
    }
}