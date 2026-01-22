using System;

using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Aggregates.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserLeftChannel" /> events.
/// </summary>
internal sealed class UserLeftChannelEventReducer : EventReducerBase<UserLeftChannel, UserAggregate>
{
    /// <inheritdoc />
    protected override UserAggregate ReduceCore(
        UserAggregate state,
        UserLeftChannel @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            ChannelIds = (state?.ChannelIds ?? []).Remove(@event.ChannelId),
        };
    }
}