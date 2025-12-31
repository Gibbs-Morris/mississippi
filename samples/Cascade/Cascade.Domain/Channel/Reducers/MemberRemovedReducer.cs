// <copyright file="MemberRemovedReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="MemberRemoved" /> event to produce a new <see cref="ChannelState" />.
/// </summary>
internal sealed class MemberRemovedReducer : Reducer<MemberRemoved, ChannelState>
{
    /// <inheritdoc />
    protected override ChannelState ReduceCore(
        ChannelState state,
        MemberRemoved eventData
    ) =>
        state with { MemberIds = state.MemberIds.Remove(eventData.UserId) };
}
