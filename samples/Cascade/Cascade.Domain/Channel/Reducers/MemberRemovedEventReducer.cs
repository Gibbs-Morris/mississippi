using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="MemberRemoved" /> event to produce a new <see cref="ChannelAggregate" />.
/// </summary>
internal sealed class MemberRemovedEventReducer : EventReducerBase<MemberRemoved, ChannelAggregate>
{
    /// <inheritdoc />
    protected override ChannelAggregate ReduceCore(
        ChannelAggregate state,
        MemberRemoved eventData
    ) =>
        state with
        {
            MemberIds = state.MemberIds.Remove(eventData.UserId),
        };
}