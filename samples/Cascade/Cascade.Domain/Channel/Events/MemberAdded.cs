// <copyright file="MemberAdded.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Channel.Events;

/// <summary>
///     Event raised when a member is added to a channel.
/// </summary>
[EventStorageName("CASCADE", "CHAT", "MEMBERADDED")]
[GenerateSerializer]
[Alias("Cascade.Domain.Channel.Events.MemberAdded")]
internal sealed record MemberAdded
{
    /// <summary>
    ///     Gets the timestamp when the member was added.
    /// </summary>
    [Id(1)]
    public required DateTimeOffset AddedAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the member added.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}