using System;

using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Aggregates.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserWentOnline" /> events.
/// </summary>
internal sealed class UserWentOnlineEventReducer : EventReducerBase<UserWentOnline, UserAggregate>
{
    /// <inheritdoc />
    protected override UserAggregate ReduceCore(
        UserAggregate state,
        UserWentOnline @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            IsOnline = true,
            LastSeenAt = @event.Timestamp,
        };
    }
}