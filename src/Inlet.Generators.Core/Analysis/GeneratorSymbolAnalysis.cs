using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Shared symbol analysis helpers for source generators.
/// </summary>
public static class GeneratorSymbolAnalysis
{
    /// <summary>
    ///     Determines whether a type has the specified attribute.
    /// </summary>
    /// <param name="typeSymbol">The symbol to inspect.</param>
    /// <param name="attributeSymbol">The attribute symbol to match.</param>
    /// <returns><c>true</c> when the type has the attribute; otherwise <c>false</c>.</returns>
    public static bool ContainsAttribute(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? attributeSymbol
    )
    {
        if (typeSymbol is null)
        {
            throw new ArgumentNullException(nameof(typeSymbol));
        }

        return attributeSymbol is not null &&
               typeSymbol.GetAttributes()
                   .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));
    }

    /// <summary>
    ///     Gets the compilation assembly plus all referenced assemblies.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The ordered assembly sequence used for symbol scanning.</returns>
    public static IEnumerable<IAssemblySymbol> GetReferencedAssemblies(
        Compilation compilation
    )
    {
        if (compilation is null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        return Enumerable.Repeat(compilation.Assembly, 1)
            .Concat(compilation.References.Select(compilation.GetAssemblyOrModuleSymbol).OfType<IAssemblySymbol>());
    }
}