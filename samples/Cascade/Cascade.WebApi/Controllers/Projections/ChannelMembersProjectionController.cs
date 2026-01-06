using Cascade.Domain.Projections.ChannelMemberList;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Api;


namespace Cascade.WebApi.Controllers.Projections;

/// <summary>
///     Controller for accessing channel member list projections.
/// </summary>
[Route("api/projections/ChannelMemberListProjection/{entityId}")]
internal sealed class ChannelMembersProjectionController : UxProjectionControllerBase<ChannelMemberListProjection>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelMembersProjectionController" /> class.
    /// </summary>
    /// <param name="uxProjectionGrainFactory">The projection grain factory.</param>
    /// <param name="logger">The logger.</param>
    public ChannelMembersProjectionController(
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<ChannelMembersProjectionController> logger
    )
        : base(uxProjectionGrainFactory, logger)
    {
    }
}
