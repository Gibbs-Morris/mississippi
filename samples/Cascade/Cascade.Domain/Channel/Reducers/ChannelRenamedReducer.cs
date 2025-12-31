// <copyright file="ChannelRenamedReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelRenamed" /> event to produce a new <see cref="ChannelState" />.
/// </summary>
internal sealed class ChannelRenamedReducer : Reducer<ChannelRenamed, ChannelState>
{
    /// <inheritdoc />
    protected override ChannelState ReduceCore(
        ChannelState state,
        ChannelRenamed eventData
    ) =>
        state with { Name = eventData.NewName };
}
