// <copyright file="ChannelArchivedReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelArchived" /> event to produce a new <see cref="ChannelAggregate" />.
/// </summary>
internal sealed class ChannelArchivedReducer : Reducer<ChannelArchived, ChannelAggregate>
{
    /// <inheritdoc />
    protected override ChannelAggregate ReduceCore(
        ChannelAggregate state,
        ChannelArchived eventData
    ) =>
        state with
        {
            IsArchived = true,
        };
}