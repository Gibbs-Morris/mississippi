using System.Collections.Immutable;

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelCreated" /> event to produce a new <see cref="ChannelAggregate" />.
/// </summary>
internal sealed class ChannelCreatedReducer : ReducerBase<ChannelCreated, ChannelAggregate>
{
    /// <inheritdoc />
    protected override ChannelAggregate ReduceCore(
        ChannelAggregate state,
        ChannelCreated eventData
    ) =>
        new()
        {
            IsCreated = true,
            ChannelId = eventData.ChannelId,
            Name = eventData.Name,
            CreatedBy = eventData.CreatedBy,
            CreatedAt = eventData.CreatedAt,
            MemberIds = ImmutableHashSet.Create(eventData.CreatedBy),
        };
}