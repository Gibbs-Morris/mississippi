using System;

using Mississippi.Inlet.Projection.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;

/// <summary>
///     Marks a record as a UX projection that should be exposed via HTTP/SignalR.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute along with <see cref="ProjectionPathAttribute" /> to a projection class to:
///         <list type="bullet">
///             <item>Generate an HTTP API controller via the ProjectionApiGenerator.</item>
///             <item>Enable subscription via Inlet/Reservoir (if using Inlet).</item>
///         </list>
///     </para>
///     <para>
///         The path from <see cref="ProjectionPathAttribute" /> is used for HTTP routing.
///         The generated controller provides the following endpoints:
///         <list type="bullet">
///             <item><c>GET api/projections/{path}/{entityId}</c> - Returns the latest projection state.</item>
///             <item><c>GET api/projections/{path}/{entityId}/version</c> - Returns the latest version.</item>
///             <item><c>GET api/projections/{path}/{entityId}/at/{version}</c> - Returns projection at version.</item>
///             <item><c>POST api/projections/{path}/batch</c> - Batch fetch (if enabled).</item>
///         </list>
///     </para>
///     <para>
///         Example usage:
///         <code>
///             [ProjectionPath("cascade/users")]
///             [UxProjection]
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
    ///     Gets or sets the authorization policy name.
    /// </summary>
    /// <remarks>
    ///     When set, the generated controller will have an <c>[Authorize(Policy = "...")]</c> attribute.
    /// </remarks>
    public string? Authorize { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether batch endpoints are enabled.
    ///     Defaults to <c>true</c>.
    /// </summary>
    public bool IsBatchEnabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets the OpenAPI tags for grouping in Swagger UI.
    /// </summary>
    public string[]? Tags { get; set; }
}