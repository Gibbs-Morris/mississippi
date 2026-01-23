using Cascade.Domain.Aggregates.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Aggregates.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="MemberAdded" /> event to produce a new <see cref="ChannelAggregate" />.
/// </summary>
internal sealed class MemberAddedEventReducer : EventReducerBase<MemberAdded, ChannelAggregate>
{
    /// <inheritdoc />
    protected override ChannelAggregate ReduceCore(
        ChannelAggregate state,
        MemberAdded eventData
    ) =>
        state with
        {
            MemberIds = state.MemberIds.Add(eventData.UserId),
        };
}