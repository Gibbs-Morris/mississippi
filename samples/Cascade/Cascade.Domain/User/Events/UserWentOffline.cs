// <copyright file="UserWentOffline.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.User.Events;

/// <summary>
///     Event raised when a user goes offline.
/// </summary>
[EventName("CASCADE", "CHAT", "USERWENTOFFLINE")]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Events.UserWentOffline")]
internal sealed record UserWentOffline
{
    /// <summary>
    ///     Gets the timestamp when the user went offline.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset Timestamp { get; init; }
}