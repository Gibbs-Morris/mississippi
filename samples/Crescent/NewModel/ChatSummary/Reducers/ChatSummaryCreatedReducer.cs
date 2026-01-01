using System;

using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.NewModel.ChatSummary.Reducers;

/// <summary>
///     Reducer that creates the initial <see cref="ChatSummaryProjection" /> from a <see cref="ChatCreated" /> event.
/// </summary>
internal sealed class ChatSummaryCreatedReducer : Reducer<ChatCreated, ChatSummaryProjection>
{
    /// <inheritdoc />
    protected override ChatSummaryProjection ReduceCore(
        ChatSummaryProjection state,
        ChatCreated @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return state with
        {
            Name = @event.Name,
        };
    }
}