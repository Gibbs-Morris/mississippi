using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Api;

/// <summary>
///     Abstract base controller for exposing UX projections via HTTP endpoints.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <typeparam name="TBrook">The brook definition type that identifies the event stream.</typeparam>
/// <remarks>
///     <para>
///         Inherit from this controller to expose a UX projection as a RESTful API.
///         The derived class must apply a route attribute and can customize serialization
///         or add additional endpoints.
///     </para>
///     <para>
///         Example usage:
///         <code>
///             [Route("api/users/{entityId}")]
///             public class UserProjectionController : UxProjectionController&lt;UserProjection, UserBrook&gt;
///             {
///                 public UserProjectionController(IUxProjectionGrainFactory factory) : base(factory) { }
///             }
///         </code>
///     </para>
///     <para>
///         This provides three endpoints:
///         <list type="bullet">
///             <item><c>GET /api/users/{entityId}</c> - Returns the latest projection state.</item>
///             <item><c>GET /api/users/{entityId}/version</c> - Returns the latest version number.</item>
///             <item><c>GET /api/users/{entityId}/at/{version}</c> - Returns the projection at a specific version.</item>
///         </list>
///     </para>
/// </remarks>
[ApiController]
public abstract class UxProjectionController<TProjection, TBrook> : ControllerBase
    where TProjection : class
    where TBrook : IBrookDefinition
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionController{TProjection, TBrook}" /> class.
    /// </summary>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains.</param>
    protected UxProjectionController(
        IUxProjectionGrainFactory uxProjectionGrainFactory
    )
    {
        UxProjectionGrainFactory = uxProjectionGrainFactory ??
                                   throw new System.ArgumentNullException(nameof(uxProjectionGrainFactory));
    }

    /// <summary>
    ///     Gets the factory for resolving UX projection grains.
    /// </summary>
    protected IUxProjectionGrainFactory UxProjectionGrainFactory { get; }

    /// <summary>
    ///     Gets the latest projection state for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     An <see cref="ActionResult{TProjection}" /> containing the projection state,
    ///     or <see cref="NotFoundResult" /> if no events exist for the entity.
    /// </returns>
    [HttpGet]
    public virtual async Task<ActionResult<TProjection>> GetAsync(
        [FromRoute] string entityId,
        CancellationToken cancellationToken = default
    )
    {
        IUxProjectionGrain<TProjection> grain =
            UxProjectionGrainFactory.GetUxProjectionGrain<TProjection, TBrook>(entityId);
        TProjection? projection = await grain.GetAsync(cancellationToken);
        if (projection is null)
        {
            return NotFound();
        }

        return Ok(projection);
    }

    /// <summary>
    ///     Gets the projection state at a specific version for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <param name="version">The specific version to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     An <see cref="ActionResult{TProjection}" /> containing the projection state at the specified version,
    ///     or <see cref="NotFoundResult" /> if the version is invalid or does not exist.
    /// </returns>
    [HttpGet("at/{version:long}")]
    public virtual async Task<ActionResult<TProjection>> GetAtVersionAsync(
        [FromRoute] string entityId,
        [FromRoute] long version,
        CancellationToken cancellationToken = default
    )
    {
        IUxProjectionGrain<TProjection> grain =
            UxProjectionGrainFactory.GetUxProjectionGrain<TProjection, TBrook>(entityId);
        BrookPosition brookPosition = new(version);
        TProjection? projection = await grain.GetAtVersionAsync(brookPosition, cancellationToken);
        if (projection is null)
        {
            return NotFound();
        }

        return Ok(projection);
    }

    /// <summary>
    ///     Gets the latest known version number for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    ///     An <see cref="ActionResult{BrookPosition}" /> containing the latest version position,
    ///     or <see cref="NotFoundResult" /> if no events exist for the entity.
    /// </returns>
    [HttpGet("version")]
    public virtual async Task<ActionResult<BrookPosition>> GetLatestVersionAsync(
        [FromRoute] string entityId,
        CancellationToken cancellationToken = default
    )
    {
        IUxProjectionGrain<TProjection> grain =
            UxProjectionGrainFactory.GetUxProjectionGrain<TProjection, TBrook>(entityId);
        BrookPosition position = await grain.GetLatestVersionAsync(cancellationToken);
        if (position.NotSet)
        {
            return NotFound();
        }

        return Ok(position);
    }
}
