namespace Mississippi.EventSourcing.UxProjections.Api.Generators.Models;

/// <summary>
///     Contains metadata about a property in a projection type.
/// </summary>
internal sealed class PropertyMetadata
{
    /// <summary>
    ///     Gets or sets the element type if this is a collection property.
    /// </summary>
    /// <remarks>
    ///     For example, for <c>ImmutableList&lt;MessageItem&gt;</c>, this would be <c>MessageItem</c>.
    /// </remarks>
    public string? CollectionElementType { get; set; }

    /// <summary>
    ///     Gets or sets the DTO type name to use in generated code.
    /// </summary>
    /// <remarks>
    ///     For primitive types, this is the same as <see cref="TypeName" />.
    ///     For custom types that need DTOs, this is the DTO type name (e.g., <c>MessageItemDto</c>).
    ///     For collections of custom types, this is the collection with DTO element (e.g., <c>IReadOnlyList&lt;MessageItemDto&gt;</c>).
    /// </remarks>
    public string DtoTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether this property is a collection.
    /// </summary>
    public bool IsCollection { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this property's type needs a DTO generated.
    /// </summary>
    /// <remarks>
    ///     True for custom types that have Orleans attributes; false for primitives and BCL types.
    /// </remarks>
    public bool NeedsDto { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the property is nullable.
    /// </summary>
    public bool Nullable { get; set; }

    /// <summary>
    ///     Gets or sets the property name.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether this property is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified type name of the property.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
}
