// <copyright file="UserProfileProjectionGrain.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain.User;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans.Runtime;


namespace Cascade.Domain.Projections.UserProfile;

/// <summary>
///     UX projection grain providing cached, read-optimized access to
///     <see cref="UserProfileProjection" /> state.
/// </summary>
/// <remarks>
///     <para>
///         This grain demonstrates the UX projection pattern for user profile data.
///         It consumes the <see cref="UserBrook" /> event stream and produces a
///         read-optimized projection for UX display purposes.
///     </para>
///     <para>
///         The grain is a stateless worker that caches the last returned projection
///         in memory. On each request, it checks the cursor position and only fetches
///         a new snapshot if the brook has advanced since the last read.
///     </para>
/// </remarks>
internal sealed class UserProfileProjectionGrain : UxProjectionGrainBase<UserProfileProjection, UserBrook>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UserProfileProjectionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
    /// <param name="logger">Logger instance.</param>
    public UserProfileProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<UserProfileProjectionGrain> logger
    )
        : base(grainContext, uxProjectionGrainFactory, logger)
    {
    }
}
