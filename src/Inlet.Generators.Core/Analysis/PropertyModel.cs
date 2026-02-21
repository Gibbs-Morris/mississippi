using System;

using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Represents a property from a source type for DTO generation.
/// </summary>
public sealed class PropertyModel
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PropertyModel" /> class.
    /// </summary>
    /// <param name="propertySymbol">The property symbol from Roslyn.</param>
    /// <param name="customTypeSuffix">The suffix for custom types (e.g., "Dto").</param>
    public PropertyModel(
        IPropertySymbol propertySymbol,
        string customTypeSuffix = "Dto"
    )
    {
        if (propertySymbol is null)
        {
            throw new ArgumentNullException(nameof(propertySymbol));
        }

        Name = propertySymbol.Name;
        SourceTypeSymbol = propertySymbol.Type;
        SourceTypeName = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        DtoTypeName = TypeAnalyzer.GetDtoTypeName(propertySymbol.Type, customTypeSuffix);
        IsFrameworkType = TypeAnalyzer.IsFrameworkType(propertySymbol.Type);
        IsCollection = TypeAnalyzer.IsCollectionType(propertySymbol.Type);
        IsEnum = TypeAnalyzer.IsEnumType(propertySymbol.Type);
        RequiresMapper = TypeAnalyzer.RequiresMapper(propertySymbol.Type);
        RequiresEnumerableMapper = TypeAnalyzer.RequiresEnumerableMapper(propertySymbol.Type);
        bool isNullableAnnotation = propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;
        bool isNullableValueType = propertySymbol.Type.IsValueType &&
                                   (propertySymbol.Type.OriginalDefinition.SpecialType ==
                                    SpecialType.System_Nullable_T);
        IsNullable = isNullableAnnotation || isNullableValueType;
        HasDefaultValue = HasPropertyDefaultValue(propertySymbol);
        DefaultValueExpression = ExtractDefaultValueExpression(propertySymbol);
        IsRequired = !IsNullable && !HasDefaultValue;

        // For collections with custom element types, extract the element info
        // Check the element type regardless of whether the collection itself is a framework type
        // (e.g., ImmutableList<MessageItem> is a framework collection containing custom elements)
        if (IsCollection)
        {
            IsImmutableArray = TypeAnalyzer.IsImmutableArrayType(propertySymbol.Type);
            ITypeSymbol? elementType = TypeAnalyzer.GetCollectionElementType(propertySymbol.Type);
            if (elementType is not null && !TypeAnalyzer.IsFrameworkType(elementType))
            {
                ElementSourceTypeName = elementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                ElementDtoTypeName = TypeAnalyzer.GetDtoTypeName(elementType, customTypeSuffix);
                ElementTypeSymbol = elementType;
            }

            ElementIsEnum = elementType is not null && TypeAnalyzer.IsEnumType(elementType);
        }
    }

    /// <summary>
    ///     Gets the default value expression as a C# literal string, or <c>null</c> if no default is declared.
    /// </summary>
    public string? DefaultValueExpression { get; }

    /// <summary>
    ///     Gets the DTO type name.
    /// </summary>
    public string DtoTypeName { get; }

    /// <summary>
    ///     Gets the element DTO type name for collections, if applicable.
    /// </summary>
    public string? ElementDtoTypeName { get; }

    /// <summary>
    ///     Gets a value indicating whether the collection element is an enum type.
    /// </summary>
    public bool ElementIsEnum { get; }

    /// <summary>
    ///     Gets the element source type name for collections, if applicable.
    /// </summary>
    public string? ElementSourceTypeName { get; }

    /// <summary>
    ///     Gets the element type symbol for collections with custom element types, if applicable.
    /// </summary>
    public ITypeSymbol? ElementTypeSymbol { get; }

    /// <summary>
    ///     Gets a value indicating whether this property has a default value.
    /// </summary>
    public bool HasDefaultValue { get; }

    /// <summary>
    ///     Gets a value indicating whether this is a collection type.
    /// </summary>
    public bool IsCollection { get; }

    /// <summary>
    ///     Gets a value indicating whether this property is an enum type.
    /// </summary>
    public bool IsEnum { get; }

    /// <summary>
    ///     Gets a value indicating whether this is a framework type.
    /// </summary>
    public bool IsFrameworkType { get; }

    /// <summary>
    ///     Gets a value indicating whether this is an ImmutableArray collection type.
    /// </summary>
    public bool IsImmutableArray { get; }

    /// <summary>
    ///     Gets a value indicating whether this property is nullable.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    ///     Gets a value indicating whether this property should be required in the DTO.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    ///     Gets the property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets a value indicating whether this property requires an enumerable mapper.
    /// </summary>
    public bool RequiresEnumerableMapper { get; }

    /// <summary>
    ///     Gets a value indicating whether this property requires a mapper.
    /// </summary>
    public bool RequiresMapper { get; }

    /// <summary>
    ///     Gets the source type name (fully qualified).
    /// </summary>
    public string SourceTypeName { get; }

    /// <summary>
    ///     Gets the source type symbol.
    /// </summary>
    public ITypeSymbol SourceTypeSymbol { get; }

    /// <summary>
    ///     Determines whether a property has a default value.
    /// </summary>
    /// <param name="propertySymbol">The property symbol.</param>
    /// <returns><c>true</c> if the property has a default value.</returns>
    private static bool HasPropertyDefaultValue(
        IPropertySymbol propertySymbol
    )
    {
        // Check for initializer in declaring syntax
        // This handles both regular property initializers (public int X { get; } = 5)
        // and positional record parameter defaults (record Foo(int X = 5))
        foreach (SyntaxReference syntaxRef in propertySymbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = syntaxRef.GetSyntax();
            string syntaxText = syntax.ToString();

            // Look for "= ..." pattern (property initializer or parameter default)
            // Handle various spacing: "= 0", " = 0", "= default", etc.
            if (syntaxText.Contains("= "))
            {
                return true;
            }
        }

        // For positional record properties, check if the associated parameter has a default
        // Positional record properties are synthesized and may have empty DeclaringSyntaxReferences
        // We need to check the containing type's primary constructor parameters
        if (propertySymbol.ContainingType is INamedTypeSymbol containingType)
        {
            // Find matching primary constructor parameter by name
            foreach (IMethodSymbol constructor in containingType.InstanceConstructors)
            {
                // Primary constructor is the one generated from record parameters
                foreach (IParameterSymbol parameter in constructor.Parameters)
                {
                    if (string.Equals(parameter.Name, propertySymbol.Name, StringComparison.Ordinal) &&
                        parameter.HasExplicitDefaultValue)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Extracts the default value expression from a property's syntax or its corresponding constructor parameter.
    /// </summary>
    /// <param name="propertySymbol">The property symbol.</param>
    /// <returns>The C# literal expression (e.g., <c>" = 100.0m"</c>), or <c>null</c> if none.</returns>
    private static string? ExtractDefaultValueExpression(
        IPropertySymbol propertySymbol
    )
    {
        // Try to extract from declaring syntax (property initializer or parameter default)
        foreach (SyntaxReference syntaxRef in propertySymbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = syntaxRef.GetSyntax();
            string syntaxText = syntax.ToString();
            int equalsIndex = syntaxText.IndexOf("= ", StringComparison.Ordinal);
            if (equalsIndex >= 0)
            {
                string valueText = syntaxText.Substring(equalsIndex + 2).Trim();
                return " = " + valueText;
            }
        }

        // For positional record properties, extract from the primary constructor parameter
        if (propertySymbol.ContainingType is INamedTypeSymbol containingType)
        {
            foreach (IMethodSymbol constructor in containingType.InstanceConstructors)
            {
                foreach (IParameterSymbol parameter in constructor.Parameters)
                {
                    if (string.Equals(parameter.Name, propertySymbol.Name, StringComparison.Ordinal) &&
                        parameter.HasExplicitDefaultValue)
                    {
                        return " = " + FormatDefaultValue(parameter.ExplicitDefaultValue, parameter.Type);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     Formats a compile-time constant value as a C# literal.
    /// </summary>
    private static string FormatDefaultValue(
        object? value,
        ITypeSymbol type
    )
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string s)
        {
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        if (value is bool b)
        {
            return b ? "true" : "false";
        }

        if (value is decimal d)
        {
            return d.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
        }

        if (value is float f)
        {
            return f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
        }

        if (value is double dbl)
        {
            return dbl.ToString(System.Globalization.CultureInfo.InvariantCulture) + "d";
        }

        if (value is long l)
        {
            return l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L";
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            return "(" + type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + ")" + value;
        }

        return value.ToString();
    }
}