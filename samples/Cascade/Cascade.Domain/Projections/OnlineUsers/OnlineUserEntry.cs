// Copyright (c) Gibbs-Morris. All rights reserved.

using System;

using Orleans;


namespace Cascade.Domain.Projections.OnlineUsers;

/// <summary>
///     Represents a user's online presence.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.OnlineUsers.OnlineUserEntry")]
internal sealed record OnlineUserEntry
{
    /// <summary>
    ///     Gets the user's display name.
    /// </summary>
    [Id(1)]
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Gets the timestamp when the user came online.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset OnlineAt { get; init; }

    /// <summary>
    ///     Gets the user identifier.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}