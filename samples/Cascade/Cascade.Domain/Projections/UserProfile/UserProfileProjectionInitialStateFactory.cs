// <copyright file="UserProfileProjectionInitialStateFactory.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Collections.Immutable;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Cascade.Domain.Projections.UserProfile;

/// <summary>
///     Factory for creating the initial state of <see cref="UserProfileProjection" /> snapshots.
/// </summary>
internal sealed class UserProfileProjectionInitialStateFactory : IInitialStateFactory<UserProfileProjection>
{
    /// <inheritdoc />
    public UserProfileProjection Create() =>
        new()
        {
            UserId = string.Empty,
            DisplayName = string.Empty,
            IsOnline = false,
            ChannelCount = 0,
            ChannelIds = ImmutableList<string>.Empty,
        };
}