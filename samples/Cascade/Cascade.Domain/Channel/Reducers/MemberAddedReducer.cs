using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="MemberAdded" /> event to produce a new <see cref="ChannelAggregate" />.
/// </summary>
internal sealed class MemberAddedReducer : ReducerBase<MemberAdded, ChannelAggregate>
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