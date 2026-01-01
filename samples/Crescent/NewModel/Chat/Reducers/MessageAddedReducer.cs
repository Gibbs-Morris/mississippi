using System;
using System.Collections.Immutable;

using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.NewModel.Chat.Reducers;

/// <summary>
///     Reducer for <see cref="MessageAdded" /> events.
///     Maintains only the last 50 messages in state.
/// </summary>
internal sealed class MessageAddedReducer : Reducer<MessageAdded, ChatState>
{
    /// <inheritdoc />
    protected override ChatState ReduceCore(
        ChatState state,
        MessageAdded @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        ChatState currentState = state ?? new();
        ChatMessage newMessage = new()
        {
            MessageId = @event.MessageId,
            Content = @event.Content,
            Author = @event.Author,
            CreatedAt = @event.CreatedAt,
        };

        // Add the new message and keep only the last 50
        ImmutableList<ChatMessage> updatedMessages = currentState.Messages.Add(newMessage);
        if (updatedMessages.Count > ChatState.MaxMessages)
        {
            updatedMessages = updatedMessages.RemoveRange(0, updatedMessages.Count - ChatState.MaxMessages);
        }

        return currentState with
        {
            Messages = updatedMessages,
            TotalMessageCount = currentState.TotalMessageCount + 1,
        };
    }
}