// <copyright file="UserJoinedChannel.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.User.Events;

/// <summary>
///     Event raised when a user joins a channel.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "USERJOINEDCHANNEL")]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Events.UserJoinedChannel")]
internal sealed record UserJoinedChannel
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the user joined the channel.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset JoinedAt { get; init; }
}