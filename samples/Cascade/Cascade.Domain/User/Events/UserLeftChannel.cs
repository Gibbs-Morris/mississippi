// <copyright file="UserLeftChannel.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.User.Events;

/// <summary>
///     Event raised when a user leaves a channel.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "USERLEFTCHANNEL")]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Events.UserLeftChannel")]
internal sealed record UserLeftChannel
{
    /// <summary>
    ///     Gets the channel identifier.
    /// </summary>
    [Id(0)]
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the user left the channel.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset LeftAt { get; init; }
}