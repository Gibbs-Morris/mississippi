using Cascade.Domain.Projections.UserProfile;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Api;


namespace Cascade.WebApi.Controllers.Projections;

/// <summary>
///     Controller for accessing user profile projections.
/// </summary>
[Route("api/projections/UserProfileProjection/{entityId}")]
internal sealed class UserProfileProjectionController : UxProjectionControllerBase<UserProfileProjection>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UserProfileProjectionController" /> class.
    /// </summary>
    /// <param name="uxProjectionGrainFactory">The projection grain factory.</param>
    /// <param name="logger">The logger.</param>
    public UserProfileProjectionController(
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<UserProfileProjectionController> logger
    )
        : base(uxProjectionGrainFactory, logger)
    {
    }
}
