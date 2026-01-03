// Copyright (c) Gibbs-Morris. All rights reserved.

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.UxProjections;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans.Runtime;


namespace Cascade.Domain.Projections.UserChannelList;

/// <summary>
///     UX projection grain providing cached, read-optimized access to
///     <see cref="UserChannelListProjection" /> state.
/// </summary>
/// <remarks>
///     <para>
///         This grain provides a list of channels a user belongs to,
///         optimized for display in the channel sidebar UI.
///     </para>
///     <para>
///         The grain consumes the User brook event stream and produces a
///         read-optimized projection for UX display purposes.
///     </para>
/// </remarks>
[BrookName("CASCADE", "CHAT", "USER")]
internal sealed class UserChannelListProjectionGrain : UxProjectionGrainBase<UserChannelListProjection>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UserChannelListProjectionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
    /// <param name="logger">Logger instance.</param>
    public UserChannelListProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<UserChannelListProjectionGrain> logger
    )
        : base(grainContext, uxProjectionGrainFactory, logger)
    {
    }
}