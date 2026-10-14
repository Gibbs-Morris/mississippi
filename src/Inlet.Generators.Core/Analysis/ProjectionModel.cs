using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Represents a projection type for code generation.
/// </summary>
public sealed class ProjectionModel
{
    private static readonly DiagnosticDescriptor ConflictingDictionaryDto = new(
        "INLETPROJ001",
        "Conflicting dictionary DTO identity",
        "Source types '{0}' and '{1}' map to the same generated dictionary DTO '{2}'. Rename a source type to use distinct DTO names.",
        "Inlet.Generators.Core",
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionModel" /> class.
    /// </summary>
    /// <param name="typeSymbol">The type symbol representing the projection.</param>
    public ProjectionModel(
        INamedTypeSymbol typeSymbol
    )
        : this(typeSymbol, string.Empty)
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
        FullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
            .SelectMany(p => p.IsCollection
                ? TypeAnalyzer.GetMappedCollectionTypes(p.SourceTypeSymbol)
                    .Select(type => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
                : new[] { p.SourceTypeName })
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
    ///     Gets a value indicating whether any property requires an ImmutableArray enumerable mapper.
    /// </summary>
    public bool HasImmutableArrayMappedProperties =>
        Properties.Any(p => p.RequiresEnumerableMapper && p.IsImmutableArray);

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

    /// <summary>
    ///     Registers custom dictionary DTO sources and reports conflicting output identities.
    /// </summary>
    /// <param name="outputNamespace">The namespace containing the generated DTOs.</param>
    /// <param name="generatedDtoTypes">The compilation-local registry of dictionary DTO source identities.</param>
    /// <returns>Diagnostics identifying distinct source types that request the same output identity.</returns>
    public ImmutableArray<Diagnostic> RegisterDictionaryDtoTypes(
        string outputNamespace,
        Dictionary<string, INamedTypeSymbol> generatedDtoTypes
    )
    {
        if (generatedDtoTypes is null)
        {
            throw new ArgumentNullException(nameof(generatedDtoTypes));
        }

        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (INamedTypeSymbol type in Properties
                     .Where(property => TypeAnalyzer.IsDictionaryType(property.SourceTypeSymbol))
                     .SelectMany(property => TypeAnalyzer.GetMappedCollectionTypes(property.SourceTypeSymbol))
                     .OfType<INamedTypeSymbol>()
                     .Where(type => type.TypeKind != TypeKind.Enum))
        {
            string identity = outputNamespace + "." + TypeAnalyzer.GetDtoTypeName(type);
            if (generatedDtoTypes.TryGetValue(identity, out INamedTypeSymbol? existing))
            {
                if (!SymbolEqualityComparer.Default.Equals(existing, type))
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            ConflictingDictionaryDto,
                            type.Locations.FirstOrDefault() ?? Location.None,
                            existing.ToDisplayString(),
                            type.ToDisplayString(),
                            identity));
                }
            }
            else
            {
                generatedDtoTypes.Add(identity, type);
            }
        }

        return diagnostics.ToImmutable();
    }
}