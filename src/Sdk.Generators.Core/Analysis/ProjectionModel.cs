using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;


namespace Mississippi.Sdk.Generators.Core.Analysis;

/// <summary>
///     Represents a projection type for code generation.
/// </summary>
public sealed class ProjectionModel
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionModel" /> class.
    /// </summary>
    /// <param name="typeSymbol">The type symbol representing the projection.</param>
    public ProjectionModel(
        INamedTypeSymbol typeSymbol
    )
        : this(typeSymbol, string.Empty, "Dto")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionModel" /> class.
    /// </summary>
    /// <param name="typeSymbol">The type symbol representing the projection.</param>
    /// <param name="projectionPath">The projection path from [ProjectionPath] attribute.</param>
    /// <param name="dtoSuffix">The suffix for generated DTOs.</param>
    public ProjectionModel(
        INamedTypeSymbol typeSymbol,
        string projectionPath,
        string dtoSuffix = "Dto"
    )
    {
        if (typeSymbol is null)
        {
            throw new ArgumentNullException(nameof(typeSymbol));
        }

        if (projectionPath is null)
        {
            throw new ArgumentNullException(nameof(projectionPath));
        }

        TypeName = typeSymbol.Name;
        FullTypeName = typeSymbol.ToDisplayString();
        Namespace = TypeAnalyzer.GetFullNamespace(typeSymbol);
        ProjectionPath = projectionPath;
        DtoTypeName = TypeAnalyzer.GetDtoTypeName(typeSymbol, dtoSuffix);

        // Derive projection name without "Projection" suffix
        ProjectionName = TypeName.EndsWith("Projection", StringComparison.Ordinal)
            ? TypeName.Substring(0, TypeName.Length - "Projection".Length)
            : TypeName;

        // Extract properties
        Properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .Where(p => !p.IsStatic)
            .Where(p => p.GetMethod is not null) // Must have getter
            .Select(p => new PropertyModel(p, dtoSuffix))
            .ToImmutableArray();

        // Determine which custom types need their own DTOs
        NestedCustomTypes = Properties.Where(p => p.RequiresMapper)
            .Select(p => p.RequiresEnumerableMapper ? p.ElementSourceTypeName! : p.SourceTypeName)
            .Distinct()
            .ToImmutableArray();
    }

    /// <summary>
    ///     Gets the DTO type name.
    /// </summary>
    public string DtoTypeName { get; }

    /// <summary>
    ///     Gets the full type name (with namespace).
    /// </summary>
    public string FullTypeName { get; }

    /// <summary>
    ///     Gets a value indicating whether any property requires an enumerable mapper.
    /// </summary>
    public bool HasEnumerableMappedProperties => Properties.Any(p => p.RequiresEnumerableMapper);

    /// <summary>
    ///     Gets a value indicating whether any property requires a mapper.
    /// </summary>
    public bool HasMappedProperties => Properties.Any(p => p.RequiresMapper);

    /// <summary>
    ///     Gets the namespace of the projection.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    ///     Gets the nested custom types that require their own DTOs.
    /// </summary>
    public ImmutableArray<string> NestedCustomTypes { get; }

    /// <summary>
    ///     Gets the projection name without the "Projection" suffix.
    /// </summary>
    public string ProjectionName { get; }

    /// <summary>
    ///     Gets the projection path from [ProjectionPath] attribute.
    /// </summary>
    public string ProjectionPath { get; }

    /// <summary>
    ///     Gets the properties of the projection.
    /// </summary>
    public ImmutableArray<PropertyModel> Properties { get; }

    /// <summary>
    ///     Gets the type name (without namespace).
    /// </summary>
    public string TypeName { get; }
}