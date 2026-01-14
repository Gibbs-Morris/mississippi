using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="DisplayNameUpdated" /> events.
/// </summary>
internal sealed class DisplayNameUpdatedEventReducer : EventReducerBase<DisplayNameUpdated, UserAggregate>
{
    /// <inheritdoc />
    protected override UserAggregate ReduceCore(
        UserAggregate state,
        DisplayNameUpdated @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            DisplayName = @event.NewDisplayName,
        };
    }
}