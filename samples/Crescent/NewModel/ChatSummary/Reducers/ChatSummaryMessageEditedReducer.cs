using System;

using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.NewModel.ChatSummary.Reducers;

/// <summary>
///     Reducer that updates <see cref="ChatSummaryProjection" /> when a message is edited.
/// </summary>
/// <remarks>
///     For the summary projection, editing a message updates the preview if it was the last message.
///     Since we don't track which message was last, we update the preview unconditionally.
/// </remarks>
internal sealed class ChatSummaryMessageEditedReducer : Reducer<MessageEdited, ChatSummaryProjection>
{
    /// <inheritdoc />
    protected override ChatSummaryProjection ReduceCore(
        ChatSummaryProjection state,
        MessageEdited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (state is null)
        {
            return state!;
        }

        // Update the preview if this could be the last message
        string preview = @event.NewContent.Length > ChatSummaryProjection.PreviewMaxLength
            ? @event.NewContent[..ChatSummaryProjection.PreviewMaxLength] + "..."
            : @event.NewContent;

        return state with
        {
            LastMessageAt = @event.EditedAt,
            LastMessagePreview = preview,
        };
    }
}
