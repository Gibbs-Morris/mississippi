// <copyright file="ChannelCreatedReducer.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Collections.Immutable;

using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Cascade.Domain.Channel.Reducers;

/// <summary>
///     Reduces the <see cref="ChannelCreated" /> event to produce a new <see cref="ChannelState" />.
/// </summary>
internal sealed class ChannelCreatedReducer : Reducer<ChannelCreated, ChannelState>
{
    /// <inheritdoc />
    protected override ChannelState ReduceCore(
        ChannelState state,
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
