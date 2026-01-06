using Cascade.Domain.Projections.ChannelMessages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Api;


namespace Cascade.WebApi.Controllers.Projections;

/// <summary>
///     Controller for accessing channel messages projections.
/// </summary>
[Route("api/projections/ChannelMessagesProjection/{entityId}")]
internal sealed class ChannelMessagesProjectionController : UxProjectionControllerBase<ChannelMessagesProjection>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelMessagesProjectionController" /> class.
    /// </summary>
    /// <param name="uxProjectionGrainFactory">The projection grain factory.</param>
    /// <param name="logger">The logger.</param>
    public ChannelMessagesProjectionController(
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<ChannelMessagesProjectionController> logger
    )
        : base(uxProjectionGrainFactory, logger)
    {
    }
}
