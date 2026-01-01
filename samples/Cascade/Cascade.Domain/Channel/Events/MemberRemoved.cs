// <copyright file="MemberRemoved.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Channel.Events;

/// <summary>
///     Event raised when a member is removed from a channel.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "MEMBERREMOVED")]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Events.MemberRemoved")]
internal sealed record MemberRemoved
{
    /// <summary>
    ///     Gets the timestamp when the member was removed.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset RemovedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the member removed.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}