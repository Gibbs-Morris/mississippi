// <copyright file="ChannelState.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Channel;

/// <summary>
///     Represents the state of a channel aggregate.
/// </summary>
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELSTATE")]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.ChannelState")]
internal sealed record ChannelState
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(1)]
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the timestamp when the channel was created.
    /// </summary>
    [Id(3)]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the channel creator.
    /// </summary>
    [Id(4)]
    public string CreatedBy { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the channel is archived.
    /// </summary>
    [Id(5)]
    public bool IsArchived { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the channel has been created.
    /// </summary>
    [Id(0)]
    public bool IsCreated { get; init; }

    /// <summary>
    ///     Gets the set of user IDs who are members of the channel.
    /// </summary>
    [Id(6)]
    public ImmutableHashSet<string> MemberIds { get; init; } = ImmutableHashSet<string>.Empty;

    /// <summary>
    ///     Gets the channel name.
    /// </summary>
    [Id(2)]
    public string Name { get; init; } = string.Empty;
}