using System;

using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.NewModel.ChatSummary.Reducers;

/// <summary>
///     Reducer that updates <see cref="ChatSummaryProjection" /> when a message is added.
/// </summary>
internal sealed class ChatSummaryMessageAddedReducer : Reducer<MessageAdded, ChatSummaryProjection>
{
    /// <inheritdoc />
    protected override ChatSummaryProjection ReduceCore(
        ChatSummaryProjection state,
        MessageAdded @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (state is null)
        {
            return state!;
        }

        string preview = @event.Content.Length > ChatSummaryProjection.PreviewMaxLength
            ? @event.Content[..ChatSummaryProjection.PreviewMaxLength] + "..."
            : @event.Content;

        return state with
        {
            MessageCount = state.MessageCount + 1,
            LastMessageAt = @event.CreatedAt,
            LastMessagePreview = preview,
            Authors = state.Authors.Add(@event.Author),
        };
    }
}
