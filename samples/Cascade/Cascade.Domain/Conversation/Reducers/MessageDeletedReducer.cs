// <copyright file="MessageDeletedReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Linq;

using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Conversation.Reducers;

/// <summary>
///     Reduces the <see cref="MessageDeleted" /> event to produce a new <see cref="ConversationState" />.
/// </summary>
internal sealed class MessageDeletedReducer : Reducer<MessageDeleted, ConversationState>
{
    /// <inheritdoc />
    protected override ConversationState ReduceCore(
        ConversationState state,
        MessageDeleted eventData
    )
    {
        int index = state.Messages.FindIndex(m => m.MessageId == eventData.MessageId);
        if (index < 0)
        {
            return state with { };
        }

        Message existingMessage = state.Messages[index];
        Message updatedMessage = existingMessage with { IsDeleted = true };

        return state with { Messages = state.Messages.SetItem(index, updatedMessage) };
    }
}
