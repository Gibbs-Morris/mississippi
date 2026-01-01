// <copyright file="ChannelArchivedReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelArchived" /> event to produce a new <see cref="ChannelState" />.
/// </summary>
internal sealed class ChannelArchivedReducer : Reducer<ChannelArchived, ChannelState>
{
    /// <inheritdoc />
    protected override ChannelState ReduceCore(
        ChannelState state,
        ChannelArchived eventData
    ) =>
        state with
        {
            IsArchived = true,
        };
}