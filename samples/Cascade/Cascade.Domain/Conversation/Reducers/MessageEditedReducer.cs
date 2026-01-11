using Cascade.Domain.Conversation.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Conversation.Reducers;

/// <summary>
///     Reduces the <see cref="MessageEdited" /> event to produce a new <see cref="ConversationAggregate" />.
/// </summary>
internal sealed class MessageEditedReducer : ReducerBase<MessageEdited, ConversationAggregate>
{
    /// <inheritdoc />
    protected override ConversationAggregate ReduceCore(
        ConversationAggregate state,
        MessageEdited eventData
    )
    {
        int index = state.Messages.FindIndex(m => m.MessageId == eventData.MessageId);
        if (index < 0)
        {
            // Must return new instance per ReducerBase requirements
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