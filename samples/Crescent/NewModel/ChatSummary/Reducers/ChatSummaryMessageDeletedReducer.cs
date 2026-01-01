using System;

using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.NewModel.ChatSummary.Reducers;

/// <summary>
///     Reducer that updates <see cref="ChatSummaryProjection" /> when a message is deleted.
/// </summary>
/// <remarks>
///     For the summary projection, we decrement the message count but cannot
///     update the preview since we don't store the full message list.
/// </remarks>
internal sealed class ChatSummaryMessageDeletedReducer : Reducer<MessageDeleted, ChatSummaryProjection>
{
    /// <inheritdoc />
    protected override ChatSummaryProjection ReduceCore(
        ChatSummaryProjection state,
        MessageDeleted @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (state is null)
        {
            return state!;
        }

        return state with
        {
            MessageCount = Math.Max(0, state.MessageCount - 1),
        };
    }
}
