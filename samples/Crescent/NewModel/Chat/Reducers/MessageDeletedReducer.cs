using System;
using System.Collections.Immutable;

using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.NewModel.Chat.Reducers;

/// <summary>
///     Reducer for <see cref="MessageDeleted" /> events.
/// </summary>
internal sealed class MessageDeletedReducer : Reducer<MessageDeleted, ChatState>
{
    /// <inheritdoc />
    protected override ChatState ReduceCore(
        ChatState state,
        MessageDeleted @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        ChatState currentState = state ?? new();

        // Find and remove the message
        int messageIndex = currentState.Messages.FindIndex(m => m.MessageId == @event.MessageId);
        if (messageIndex < 0)
        {
            // Message not in the last 50, return a copy with no changes
            return currentState with
            {
            };
        }

        ImmutableList<ChatMessage> updatedMessages = currentState.Messages.RemoveAt(messageIndex);
        return currentState with
        {
            Messages = updatedMessages,
        };
    }
}