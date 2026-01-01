using System;
using System.Collections.Immutable;

using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.NewModel.Chat.Reducers;

/// <summary>
///     Reducer for <see cref="MessageEdited" /> events.
/// </summary>
internal sealed class MessageEditedReducer : Reducer<MessageEdited, ChatState>
{
    /// <inheritdoc />
    protected override ChatState ReduceCore(
        ChatState state,
        MessageEdited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);

        ChatState currentState = state ?? new();

        // Find and update the message
        int messageIndex = currentState.Messages
            .FindIndex(m => m.MessageId == @event.MessageId);

        if (messageIndex < 0)
        {
            // Message not in the last 50, return a copy with no changes
            return currentState with { };
        }

        ChatMessage existingMessage = currentState.Messages[messageIndex];
        ChatMessage updatedMessage = existingMessage with
        {
            Content = @event.NewContent,
            EditedAt = @event.EditedAt,
        };

        ImmutableList<ChatMessage> updatedMessages = currentState.Messages
            .SetItem(messageIndex, updatedMessage);

        return currentState with
        {
            Messages = updatedMessages,
        };
    }
}
