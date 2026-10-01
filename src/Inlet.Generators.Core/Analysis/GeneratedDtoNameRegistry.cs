using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Tracks the source symbols represented by generated DTO declarations.
/// </summary>
public sealed class GeneratedDtoNameRegistry
{
    private static readonly DiagnosticDescriptor DtoNameCollisionDescriptor = new(
        "INLETDTO001",
        "Generated DTO name collision",
        "Generated DTO '{0}' maps to both '{1}' and '{2}'. Rename one source type to produce distinct DTO names.",
        "Mississippi.Inlet.Projections",
        DiagnosticSeverity.Error,
        true);

    private Dictionary<string, INamedTypeSymbol> GeneratedTypes { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     Reserves a DTO name for its source symbol and reports conflicting source types.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="targetNamespace">The namespace of the generated DTO.</param>
    /// <param name="dtoName">The generated DTO name.</param>
    /// <param name="sourceType">The source type represented by the DTO.</param>
    /// <returns>Whether a new declaration should be generated.</returns>
    public bool TryRegister(
        SourceProductionContext context,
        string targetNamespace,
        string dtoName,
        INamedTypeSymbol sourceType
    )
    {
        if (targetNamespace is null)
        {
            throw new ArgumentNullException(nameof(targetNamespace));
        }

        if (dtoName is null)
        {
            throw new ArgumentNullException(nameof(dtoName));
        }

        if (sourceType is null)
        {
            throw new ArgumentNullException(nameof(sourceType));
        }

        string key = $"{targetNamespace}.{dtoName}";
        if (!GeneratedTypes.TryGetValue(key, out INamedTypeSymbol? existingType))
        {
            GeneratedTypes.Add(key, sourceType);
            return true;
        }

        if (!SymbolEqualityComparer.Default.Equals(existingType, sourceType))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DtoNameCollisionDescriptor,
                    sourceType.Locations.FirstOrDefault(location => location.IsInSource) ?? Location.None,
                    key,
                    existingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        return false;
    }
}