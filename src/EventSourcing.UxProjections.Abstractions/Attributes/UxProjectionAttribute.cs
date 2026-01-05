using System;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;

/// <summary>
///     Marks a record as a UX projection that should be exposed via HTTP/SignalR.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute to a projection class to:
///         <list type="bullet">
///             <item>Generate an HTTP API controller via the ProjectionApiGenerator.</item>
///             <item>Register IRipple/IRipplePool services via the RipplesGenerator (if using Ripples).</item>
///             <item>Generate route helpers in the RouteRegistry.</item>
///         </list>
///     </para>
///     <para>
///         The generated controller provides the following endpoints:
///         <list type="bullet">
///             <item><c>GET api/projections/{Route}/{entityId}</c> - Returns the latest projection state.</item>
///             <item><c>GET api/projections/{Route}/{entityId}/version</c> - Returns the latest version.</item>
///             <item><c>GET api/projections/{Route}/{entityId}/at/{version}</c> - Returns projection at version.</item>
///             <item><c>POST api/projections/{Route}/batch</c> - Batch fetch (if enabled).</item>
///         </list>
///     </para>
///     <para>
///         Example usage:
///         <code>
///             [UxProjection("users")]
///             [BrookName("user-events")]
///             public sealed class UserProjection
///             {
///                 public string Name { get; set; }
///                 public int Count { get; set; }
///             }
///         </code>
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class UxProjectionAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionAttribute" /> class.
    /// </summary>
    /// <param name="route">
    ///     The HTTP route segment for this projection.
    ///     The full route will be <c>api/projections/{route}/{entityId}</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="route" /> is null.</exception>
    public UxProjectionAttribute(
        string route
    )
    {
        ArgumentNullException.ThrowIfNull(route);
        Route = route;
    }

    /// <summary>
    ///     Gets or sets the authorization policy name.
    /// </summary>
    /// <remarks>
    ///     When set, the generated controller will have an <c>[Authorize(Policy = "...")]</c> attribute.
    /// </remarks>
    public string? Authorize { get; set; }

    /// <summary>
    ///     Gets or sets the brook name to read from (defaults to projection type name).
    /// </summary>
    public string? BrookName { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether batch endpoints are enabled.
    ///     Defaults to <c>true</c>.
    /// </summary>
    public bool IsBatchEnabled { get; set; } = true;

    /// <summary>
    ///     Gets the HTTP route segment for this projection.
    /// </summary>
    public string Route { get; }

    /// <summary>
    ///     Gets or sets the OpenAPI tags for grouping in Swagger UI.
    /// </summary>
    public string[]? Tags { get; set; }
}