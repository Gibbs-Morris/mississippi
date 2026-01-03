// Copyright (c) Gibbs-Morris. All rights reserved.

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.UxProjections;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans.Runtime;


namespace Cascade.Domain.Projections.ChannelMemberList;

/// <summary>
///     UX projection grain providing cached, read-optimized access to
///     <see cref="ChannelMemberListProjection" /> state.
/// </summary>
/// <remarks>
///     <para>
///         This grain provides the list of members for a channel,
///         optimized for display in the member sidebar UI.
///     </para>
///     <para>
///         The grain consumes the Channel brook event stream and produces a
///         read-optimized projection for UX display purposes.
///     </para>
/// </remarks>
[BrookName("CASCADE", "CHAT", "CHANNEL")]
internal sealed class ChannelMemberListProjectionGrain : UxProjectionGrainBase<ChannelMemberListProjection>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelMemberListProjectionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
    /// <param name="logger">Logger instance.</param>
    public ChannelMemberListProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<ChannelMemberListProjectionGrain> logger
    )
        : base(grainContext, uxProjectionGrainFactory, logger)
    {
    }
}