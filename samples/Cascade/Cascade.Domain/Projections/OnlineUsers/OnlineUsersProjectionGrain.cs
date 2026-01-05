using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.EventSourcing.UxProjections;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans.Runtime;


namespace Cascade.Domain.Projections.OnlineUsers;

/// <summary>
///     UX projection grain providing cached, read-optimized access to
///     <see cref="OnlineUsersProjection" /> state.
/// </summary>
/// <remarks>
///     <para>
///         This grain provides the online status for a specific user,
///         optimized for display in presence indicators.
///     </para>
///     <para>
///         The grain consumes the User brook event stream and produces a
///         read-optimized projection for UX display purposes.
///     </para>
/// </remarks>
[BrookName("CASCADE", "CHAT", "USER")]
internal sealed class OnlineUsersProjectionGrain : UxProjectionGrainBase<OnlineUsersProjection>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OnlineUsersProjectionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
    /// <param name="logger">Logger instance.</param>
    public OnlineUsersProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<OnlineUsersProjectionGrain> logger
    )
        : base(grainContext, uxProjectionGrainFactory, logger)
    {
    }
}