using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserJoinedChannel" /> events.
/// </summary>
internal sealed class UserJoinedChannelReducer : ReducerBase<UserJoinedChannel, UserAggregate>
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