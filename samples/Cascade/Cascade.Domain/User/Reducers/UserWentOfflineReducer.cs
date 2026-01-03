using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserWentOffline" /> events.
/// </summary>
internal sealed class UserWentOfflineReducer : Reducer<UserWentOffline, UserAggregate>
{
    /// <inheritdoc />
    protected override UserAggregate ReduceCore(
        UserAggregate state,
        UserWentOffline @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            IsOnline = false,
            LastSeenAt = @event.Timestamp,
        };
    }
}