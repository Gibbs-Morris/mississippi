using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Diagnostics for generated projection DTO and mapper contracts.
/// </summary>
public static class GeneratedProjectionDiagnostics
{
    /// <summary>
    ///     Reports an enum collection without a compatible DTO materializer.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedEnumCollection = new(
        "INLETDTO002",
        "Unsupported enum collection shape",
        "Property '{0}' uses enum collection '{1}' without a compatible DTO materializer. Use a one-dimensional array or a supported list or set collection.",
        "Mississippi.Inlet.Projections",
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     Reports unsupported collection properties once and prevents their projection output.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="sourceType">The projection source type.</param>
    /// <param name="model">The projection model and nested collection elements.</param>
    /// <param name="reportedProperties">Properties reported during this output pass.</param>
    /// <param name="isUnsupported">The consumer's unsupported collection predicate.</param>
    /// <returns>Whether the projection's collection properties are supported.</returns>
    public static bool ValidateEnumCollectionShapes(
        SourceProductionContext context,
        INamedTypeSymbol sourceType,
        ProjectionModel model,
        ISet<IPropertySymbol> reportedProperties,
        Func<IPropertySymbol, bool> isUnsupported
    )
    {
        if (sourceType is null)
        {
            throw new ArgumentNullException(nameof(sourceType));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (reportedProperties is null)
        {
            throw new ArgumentNullException(nameof(reportedProperties));
        }

        if (isUnsupported is null)
        {
            throw new ArgumentNullException(nameof(isUnsupported));
        }

        bool valid = true;
        foreach (IPropertySymbol property in GetPublicProperties(sourceType, model).Where(isUnsupported))
        {
            if (reportedProperties.Add(property))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        UnsupportedEnumCollection,
                        property.Locations.FirstOrDefault(location => location.IsInSource) ?? Location.None,
                        property.Name,
                        property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }

            valid = false;
        }

        return valid;
    }

    private static IEnumerable<IPropertySymbol> GetPublicProperties(
        INamedTypeSymbol sourceType,
        ProjectionModel model
    ) =>
        sourceType.GetMembers()
            .OfType<IPropertySymbol>()
            .Concat(
                model.Properties.SelectMany(property =>
                    property.ElementTypeSymbol is INamedTypeSymbol { TypeKind: not TypeKind.Enum } nestedType
                        ? nestedType.GetMembers().OfType<IPropertySymbol>()
                        : Enumerable.Empty<IPropertySymbol>()))
            .Where(property => (property.DeclaredAccessibility == Accessibility.Public) &&
                               !property.IsStatic &&
                               property.GetMethod is not null)
            .Distinct(SymbolEqualityComparer.Default)
            .OfType<IPropertySymbol>();
}