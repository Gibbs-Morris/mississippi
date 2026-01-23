using System;

using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Projections.UserProfile.Reducers;

/// <summary>
///     Reduces the <see cref="DisplayNameUpdated" /> event to update the display name
///     in the <see cref="UserProfileProjection" />.
/// </summary>
internal sealed class DisplayNameUpdatedProjectionEventReducer
    : EventReducerBase<DisplayNameUpdated, UserProfileProjection>
{
    /// <inheritdoc />
    protected override UserProfileProjection ReduceCore(
        UserProfileProjection state,
        DisplayNameUpdated eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            DisplayName = eventData.NewDisplayName,
        };
    }
}