using Cascade.Domain.Projections.UserChannelList;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Api;


namespace Cascade.WebApi.Controllers.Projections;

/// <summary>
///     Controller for accessing user channel list projections.
/// </summary>
[Route("api/projections/UserChannelListProjection/{entityId}")]
internal sealed class UserChannelsProjectionController : UxProjectionControllerBase<UserChannelListProjection>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UserChannelsProjectionController" /> class.
    /// </summary>
    /// <param name="uxProjectionGrainFactory">The projection grain factory.</param>
    /// <param name="logger">The logger.</param>
    public UserChannelsProjectionController(
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<UserChannelsProjectionController> logger
    )
        : base(uxProjectionGrainFactory, logger)
    {
    }
}
