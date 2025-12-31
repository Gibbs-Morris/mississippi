// <copyright file="UserProfileProjection.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Cascade.Domain.Projections.UserProfile;

/// <summary>
///     Read-optimized projection of user profile information for UX display.
/// </summary>
/// <remarks>
///     <para>
///         This projection provides a denormalized view of user state optimized
///         for display in UI components like member lists and profile cards.
///     </para>
///     <para>
///         Subscribes to events from <see cref="User.UserBrook" />:
///         UserRegistered, DisplayNameUpdated, UserWentOnline, UserWentOffline,
///         UserJoinedChannel, UserLeftChannel.
///     </para>
/// </remarks>
[SnapshotName("CASCADE", "CHAT", "USERPROFILE")]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.UserProfile.UserProfileProjection")]
internal sealed record UserProfileProjection
{
    /// <summary>
    ///     Gets the user identifier.
    /// </summary>
    [Id(0)]
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the user's display name.
    /// </summary>
    [Id(1)]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the user is currently online.
    /// </summary>
    [Id(2)]
    public bool IsOnline { get; init; }

    /// <summary>
    ///     Gets the time when the user registered.
    /// </summary>
    [Id(3)]
    public DateTimeOffset RegisteredAt { get; init; }

    /// <summary>
    ///     Gets the time when the user last went online.
    /// </summary>
    [Id(4)]
    public DateTimeOffset? LastOnlineAt { get; init; }

    /// <summary>
    ///     Gets the number of channels the user has joined.
    /// </summary>
    [Id(5)]
    public int ChannelCount { get; init; }

    /// <summary>
    ///     Gets the list of channel IDs the user has joined.
    /// </summary>
    [Id(6)]
    public ImmutableList<string> ChannelIds { get; init; } = ImmutableList<string>.Empty;
}
