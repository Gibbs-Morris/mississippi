using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelRenamed" /> event to produce a new <see cref="ChannelAggregate" />.
/// </summary>
internal sealed class ChannelRenamedReducer : Reducer<ChannelRenamed, ChannelAggregate>
{
    /// <inheritdoc />
    protected override ChannelAggregate ReduceCore(
        ChannelAggregate state,
        ChannelRenamed eventData
    ) =>
        state with
        {
            Name = eventData.NewName,
        };
}