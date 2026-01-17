using System.Collections.Generic;


namespace Mississippi.EventSourcing.UxProjections.Api.Generators.Models;

/// <summary>
///     Contains extracted information about a projection type marked with [UxProjection].
/// </summary>
internal sealed class ProjectionApiInfo
{
    /// <summary>
    ///     Gets or sets the authorization policy name, if any.
    /// </summary>
    public string? Authorize { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the projection.
    /// </summary>
    public string FullTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether batch endpoints are enabled.
    /// </summary>
    public bool IsBatchEnabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets the namespace of the projection type.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the nested types that require DTO generation.
    /// </summary>
    public List<NestedTypeMetadata> NestedTypes { get; set; } = [];

    /// <summary>
    ///     Gets or sets the properties of the projection.
    /// </summary>
    public List<PropertyMetadata> Properties { get; set; } = [];

    /// <summary>
    ///     Gets or sets the HTTP route for the projection controller.
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the OpenAPI tags.
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    ///     Gets or sets the simple type name (without namespace).
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
}