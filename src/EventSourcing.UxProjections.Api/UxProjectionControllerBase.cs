using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Api;

/// <summary>
///     Abstract base controller for exposing UX projections via HTTP endpoints.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
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
///             public class UserProjectionController : UxProjectionControllerBase&lt;UserProjection&gt;
///             {
///                 public UserProjectionController(
///                     IUxProjectionGrainFactory factory,
///                     ILogger&lt;UxProjectionControllerBase&lt;UserProjection&gt;&gt; logger) : base(factory, logger) { }
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
public abstract class UxProjectionControllerBase<TProjection> : ControllerBase
    where TProjection : class
{
    private static readonly string ProjectionTypeName = typeof(TProjection).Name;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionControllerBase{TProjection}" />
    ///     class.
    /// </summary>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    protected UxProjectionControllerBase(
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<UxProjectionControllerBase<TProjection>> logger
    )
    {
        UxProjectionGrainFactory = uxProjectionGrainFactory ??
                                   throw new ArgumentNullException(nameof(uxProjectionGrainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets the logger for diagnostic output.
    /// </summary>
    protected ILogger<UxProjectionControllerBase<TProjection>> Logger { get; }

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
    ///     or <see cref="NotFoundResult" /> if no events exist for the entity,
    ///     or <see cref="StatusCodeResult" /> with status 304 if the ETag matches.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This endpoint supports HTTP caching via ETag headers:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             The response includes an <c>ETag</c> header containing the projection version.
    ///         </item>
    ///         <item>
    ///             Clients can send an <c>If-None-Match</c> header to receive a 304 Not Modified
    ///             response if the projection has not changed.
    ///         </item>
    ///         <item>
    ///             The <c>Cache-Control</c> header is set to <c>private, must-revalidate</c>
    ///             to ensure clients always validate with the server.
    ///         </item>
    ///     </list>
    /// </remarks>
    [HttpGet]
    public virtual async Task<ActionResult<TProjection>> GetAsync(
        [FromRoute] string entityId,
        CancellationToken cancellationToken = default
    )
    {
        Logger.GettingLatestProjection(entityId, ProjectionTypeName);
        IUxProjectionGrain<TProjection> grain = UxProjectionGrainFactory.GetUxProjectionGrain<TProjection>(entityId);

        // Get the latest version first for ETag support
        BrookPosition position = await grain.GetLatestVersionAsync(cancellationToken);
        if (position.NotSet)
        {
            Logger.ProjectionNotFound(entityId, ProjectionTypeName);
            return NotFound();
        }

        string currentETag = $"\"{position.Value}\"";

        // Check If-None-Match header for conditional GET
        string? ifNoneMatch = Request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch) && (ifNoneMatch == currentETag))
        {
            Logger.ProjectionNotModified(entityId, position.Value, ProjectionTypeName);
            return StatusCode(304);
        }

        // Fetch the projection data
        TProjection? projection = await grain.GetAsync(cancellationToken);
        if (projection is null)
        {
            Logger.ProjectionNotFound(entityId, ProjectionTypeName);
            return NotFound();
        }

        // Set caching headers
        Response.Headers.ETag = currentETag;
        Response.Headers.CacheControl = "private, must-revalidate";
        Logger.ProjectionRetrieved(entityId, ProjectionTypeName);
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
        Logger.GettingProjectionAtVersion(entityId, version, ProjectionTypeName);
        IUxProjectionGrain<TProjection> grain = UxProjectionGrainFactory.GetUxProjectionGrain<TProjection>(entityId);
        BrookPosition brookPosition = new(version);
        TProjection? projection = await grain.GetAtVersionAsync(brookPosition, cancellationToken);
        if (projection is null)
        {
            Logger.ProjectionAtVersionNotFound(entityId, version, ProjectionTypeName);
            return NotFound();
        }

        Logger.ProjectionAtVersionRetrieved(entityId, version, ProjectionTypeName);
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
        Logger.GettingLatestVersion(entityId, ProjectionTypeName);
        IUxProjectionGrain<TProjection> grain = UxProjectionGrainFactory.GetUxProjectionGrain<TProjection>(entityId);
        BrookPosition position = await grain.GetLatestVersionAsync(cancellationToken);
        if (position.NotSet)
        {
            Logger.NoVersionFound(entityId, ProjectionTypeName);
            return NotFound();
        }

        Logger.LatestVersionRetrieved(entityId, position.Value, ProjectionTypeName);
        return Ok(position);
    }
}