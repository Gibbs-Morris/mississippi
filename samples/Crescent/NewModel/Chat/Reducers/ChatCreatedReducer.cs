using System;

using Crescent.NewModel.Chat.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.NewModel.Chat.Reducers;

/// <summary>
///     Reducer for <see cref="ChatCreated" /> events.
/// </summary>
internal sealed class ChatCreatedReducer : Reducer<ChatCreated, ChatState>
{
    /// <inheritdoc />
    protected override ChatState ReduceCore(
        ChatState state,
        ChatCreated @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            IsCreated = true,
            Name = @event.Name,
        };
    }
}
