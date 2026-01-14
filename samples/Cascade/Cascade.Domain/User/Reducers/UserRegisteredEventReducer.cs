using System;

using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.User.Reducers;

/// <summary>
///     Reducer for <see cref="UserRegistered" /> events.
/// </summary>
internal sealed class UserRegisteredEventReducer : EventReducerBase<UserRegistered, UserAggregate>
{
    /// <summary>
    ///     The default state for a new user aggregate.
    /// </summary>
    private static readonly UserAggregate DefaultState = new();

    /// <inheritdoc />
    protected override UserAggregate ReduceCore(
        UserAggregate state,
        UserRegistered @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? DefaultState) with
        {
            IsRegistered = true,
            UserId = @event.UserId,
            DisplayName = @event.DisplayName,
            RegisteredAt = @event.RegisteredAt,
        };
    }
}