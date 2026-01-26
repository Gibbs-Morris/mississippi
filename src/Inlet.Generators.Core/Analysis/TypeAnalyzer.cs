using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Provides utilities for analyzing types in source generators.
/// </summary>
public static class TypeAnalyzer
{
    /// <summary>
    ///     Namespace prefixes that are considered framework types (no DTO conversion needed).
    /// </summary>
    private static readonly ImmutableArray<string> FrameworkNamespacePrefixes = ImmutableArray.Create(
        "System",
        "Microsoft",
        "Orleans",
        "Newtonsoft");

    /// <summary>
    ///     Gets a boolean property value from an attribute.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="propertyName">The name of the property to read.</param>
    /// <param name="defaultValue">The default value if the property is not set.</param>
    /// <returns>The property value, or the default if not set.</returns>
    public static bool GetBooleanProperty(
        AttributeData attribute,
        string propertyName,
        bool defaultValue = true
    )
    {
        if (attribute is null)
        {
            return defaultValue;
        }

        KeyValuePair<string, TypedConstant> kvp = attribute.NamedArguments.FirstOrDefault(x => x.Key == propertyName);
        if (kvp.Key is null || kvp.Value.IsNull)
        {
            return defaultValue;
        }

        return kvp.Value.Value is bool value ? value : defaultValue;
    }

    /// <summary>
    ///     Gets the element type of a collection.
    /// </summary>
    /// <param name="typeSymbol">The collection type symbol.</param>
    /// <returns>The element type, or <c>null</c> if not a collection.</returns>
    public static ITypeSymbol? GetCollectionElementType(
        ITypeSymbol typeSymbol
    )
    {
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType && (namedType.TypeArguments.Length > 0))
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    /// <summary>
    ///     Gets the DTO type name for a given type.
    /// </summary>
    /// <param name="typeSymbol">The type symbol.</param>
    /// <param name="customTypeSuffix">The suffix to add to custom types (e.g., "Dto").</param>
    /// <returns>The DTO type name.</returns>
    public static string GetDtoTypeName(
        ITypeSymbol typeSymbol,
        string customTypeSuffix = "Dto"
    )
    {
        if (typeSymbol is null)
        {
            throw new ArgumentNullException(nameof(typeSymbol));
        }

        // Handle array types
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            string elementDtoName = GetDtoTypeName(arrayType.ElementType, customTypeSuffix);
            return elementDtoName + "[]";
        }

        // Handle generic types (collections)
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            string genericTypeName = namedType.ConstructedFrom.Name;
            ImmutableArray<ITypeSymbol> typeArgs = namedType.TypeArguments;

            // Map each type argument
            IEnumerable<string> mappedArgs = typeArgs.Select(arg => GetDtoTypeName(arg, customTypeSuffix));
            return $"{genericTypeName}<{string.Join(", ", mappedArgs)}>";
        }

        // Non-generic types
        if (IsFrameworkType(typeSymbol))
        {
            return typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        // Custom type - add suffix
        string typeName = typeSymbol.Name;

        // Remove common suffixes before adding Dto
        typeName = RemoveKnownSuffixes(typeName);
        return typeName + customTypeSuffix;
    }

    /// <summary>
    ///     Gets the full namespace of a type symbol.
    /// </summary>
    /// <param name="typeSymbol">The type symbol.</param>
    /// <returns>The full namespace, or an empty string if none.</returns>
    public static string GetFullNamespace(
        ITypeSymbol typeSymbol
    )
    {
        if (typeSymbol is null)
        {
            return string.Empty;
        }

        INamespaceSymbol? ns = typeSymbol.ContainingNamespace;
        if (ns is null || ns.IsGlobalNamespace)
        {
            return string.Empty;
        }

        return ns.ToDisplayString();
    }

    /// <summary>
    ///     Gets a nullable boolean property value from an attribute.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="propertyName">The name of the property to read.</param>
    /// <returns>The property value, or null if not set.</returns>
    public static bool? GetNullableBooleanProperty(
        AttributeData? attribute,
        string propertyName
    )
    {
        if (attribute is null)
        {
            return null;
        }

        KeyValuePair<string, TypedConstant> kvp = attribute.NamedArguments.FirstOrDefault(x => x.Key == propertyName);
        if (kvp.Key is null || kvp.Value.IsNull)
        {
            return null;
        }

        return kvp.Value.Value as bool?;
    }

    /// <summary>
    ///     Gets a string property value from an attribute.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="propertyName">The name of the property to read.</param>
    /// <returns>The property value, or null if not set.</returns>
    public static string? GetStringProperty(
        AttributeData? attribute,
        string propertyName
    )
    {
        if (attribute is null)
        {
            return null;
        }

        return attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == propertyName).Value.Value?.ToString();
    }

    /// <summary>
    ///     Determines whether a type is a collection type.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to analyze.</param>
    /// <returns><c>true</c> if the type is a collection type; otherwise, <c>false</c>.</returns>
    public static bool IsCollectionType(
        ITypeSymbol typeSymbol
    )
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // Array types
        if (typeSymbol is IArrayTypeSymbol)
        {
            return true;
        }

        // Check for generic collection interfaces
        if (!namedType.IsGenericType)
        {
            return false;
        }

        string typeName = namedType.ConstructedFrom.ToDisplayString();
        return IsKnownCollectionType(typeName);
    }

    /// <summary>
    ///     Determines whether a type is an enum.
    /// </summary>
    /// <param name="typeSymbol">The type symbol.</param>
    /// <returns><c>true</c> if the type is an enum; otherwise, <c>false</c>.</returns>
    public static bool IsEnumType(
        ITypeSymbol typeSymbol
    ) =>
        typeSymbol is not null && (typeSymbol.TypeKind == TypeKind.Enum);

    /// <summary>
    ///     Determines whether a type is a framework type that should not be converted to a DTO.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to analyze.</param>
    /// <returns><c>true</c> if the type is a framework type; otherwise, <c>false</c>.</returns>
    public static bool IsFrameworkType(
        ITypeSymbol typeSymbol
    )
    {
        if (typeSymbol is null)
        {
            return true;
        }

        // Special types (primitives) are always framework types
        if (typeSymbol.SpecialType != SpecialType.None)
        {
            return true;
        }

        // Check namespace
        string containingNamespace = GetFullNamespace(typeSymbol);
        if (string.IsNullOrEmpty(containingNamespace))
        {
            return true;
        }

        return FrameworkNamespacePrefixes.Any(prefix =>
            containingNamespace.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Determines whether a type is an ImmutableArray.
    /// </summary>
    /// <param name="typeSymbol">The type symbol.</param>
    /// <returns><c>true</c> if the type is an ImmutableArray; otherwise, <c>false</c>.</returns>
    public static bool IsImmutableArrayType(
        ITypeSymbol typeSymbol
    )
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return false;
        }

        string typeName = namedType.OriginalDefinition.ToDisplayString();
        return typeName == "System.Collections.Immutable.ImmutableArray<T>";
    }

    /// <summary>
    ///     Determines whether a property requires an enumerable mapper.
    /// </summary>
    /// <param name="propertyType">The property type symbol.</param>
    /// <returns><c>true</c> if an enumerable mapper is required; otherwise, <c>false</c>.</returns>
    public static bool RequiresEnumerableMapper(
        ITypeSymbol propertyType
    )
    {
        if (!IsCollectionType(propertyType))
        {
            return false;
        }

        ITypeSymbol? elementType = GetCollectionElementType(propertyType);
        return elementType is not null && !IsFrameworkType(elementType);
    }

    /// <summary>
    ///     Determines whether a property requires a mapper for DTO conversion.
    /// </summary>
    /// <param name="propertyType">The property type symbol.</param>
    /// <returns><c>true</c> if a mapper is required; otherwise, <c>false</c>.</returns>
    public static bool RequiresMapper(
        ITypeSymbol propertyType
    )
    {
        // Check if it's a collection with custom element type
        if (IsCollectionType(propertyType))
        {
            ITypeSymbol? elementType = GetCollectionElementType(propertyType);
            return elementType is not null && !IsFrameworkType(elementType);
        }

        // Direct custom type
        return !IsFrameworkType(propertyType);
    }

    /// <summary>
    ///     Checks if a type name is a known collection type.
    /// </summary>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns><c>true</c> if it's a known collection type.</returns>
    private static bool IsKnownCollectionType(
        string typeName
    ) =>
        typeName.StartsWith("System.Collections.", StringComparison.Ordinal) ||
        typeName.StartsWith("System.Collections.Generic.", StringComparison.Ordinal) ||
        typeName.StartsWith("System.Collections.Immutable.", StringComparison.Ordinal) ||
        (typeName == "System.Collections.Generic.List<T>") ||
        (typeName == "System.Collections.Generic.IList<T>") ||
        (typeName == "System.Collections.Generic.ICollection<T>") ||
        (typeName == "System.Collections.Generic.IEnumerable<T>") ||
        (typeName == "System.Collections.Generic.IReadOnlyList<T>") ||
        (typeName == "System.Collections.Generic.IReadOnlyCollection<T>") ||
        (typeName == "System.Collections.Immutable.ImmutableArray<T>") ||
        (typeName == "System.Collections.Immutable.ImmutableList<T>");

    /// <summary>
    ///     Removes known suffixes from a type name.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <returns>The type name without known suffixes.</returns>
    private static string RemoveKnownSuffixes(
        string typeName
    )
    {
        string[] suffixes = { "Projection", "Aggregate", "State" };
        string result = suffixes.Where(suffix => typeName.EndsWith(suffix, StringComparison.Ordinal))
            .Aggregate(
                typeName,
                (
                    current,
                    suffix
                ) => current.Substring(0, current.Length - suffix.Length));
        return result == typeName ? typeName : result;
    }
}