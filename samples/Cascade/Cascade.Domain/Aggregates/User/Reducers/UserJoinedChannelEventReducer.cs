using System;

using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Aggregates.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserJoinedChannel" /> events.
/// </summary>
internal sealed class UserJoinedChannelEventReducer : EventReducerBase<UserJoinedChannel, UserAggregate>
{
    /// <inheritdoc />
    protected override UserAggregate ReduceCore(
        UserAggregate state,
        UserJoinedChannel @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            ChannelIds = (state?.ChannelIds ?? []).Add(@event.ChannelId),
        };
    }
}