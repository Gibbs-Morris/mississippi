using System.Collections.Generic;


namespace Mississippi.EventSourcing.UxProjections.Api.Generators.Models;

/// <summary>
///     Contains metadata about a nested type that needs a DTO generated.
/// </summary>
internal sealed class NestedTypeMetadata
{
    /// <summary>
    ///     Gets or sets the fully qualified type name.
    /// </summary>
    public string FullTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the properties of this type.
    /// </summary>
    public List<PropertyMetadata> Properties { get; set; } = [];

    /// <summary>
    ///     Gets or sets the simple type name.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
}