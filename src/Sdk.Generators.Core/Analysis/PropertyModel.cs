using System;

using Microsoft.CodeAnalysis;


namespace Mississippi.Sdk.Generators.Core.Analysis;

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
        SourceTypeName = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        DtoTypeName = TypeAnalyzer.GetDtoTypeName(propertySymbol.Type, customTypeSuffix);
        IsFrameworkType = TypeAnalyzer.IsFrameworkType(propertySymbol.Type);
        IsCollection = TypeAnalyzer.IsCollectionType(propertySymbol.Type);
        RequiresMapper = TypeAnalyzer.RequiresMapper(propertySymbol.Type);
        RequiresEnumerableMapper = TypeAnalyzer.RequiresEnumerableMapper(propertySymbol.Type);
        bool isNullableAnnotation = propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;
        bool isNullableValueType = propertySymbol.Type.IsValueType &&
                                   (propertySymbol.Type.OriginalDefinition.SpecialType ==
                                    SpecialType.System_Nullable_T);
        IsNullable = isNullableAnnotation || isNullableValueType;
        HasDefaultValue = HasPropertyDefaultValue(propertySymbol);
        IsRequired = !IsNullable && !HasDefaultValue;

        // For collections with custom element types, extract the element info
        if (IsCollection && !IsFrameworkType)
        {
            ITypeSymbol? elementType = TypeAnalyzer.GetCollectionElementType(propertySymbol.Type);
            if (elementType is not null)
            {
                ElementSourceTypeName = elementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                ElementDtoTypeName = TypeAnalyzer.GetDtoTypeName(elementType, customTypeSuffix);
            }
        }
    }

    /// <summary>
    ///     Gets the DTO type name.
    /// </summary>
    public string DtoTypeName { get; }

    /// <summary>
    ///     Gets the element DTO type name for collections, if applicable.
    /// </summary>
    public string? ElementDtoTypeName { get; }

    /// <summary>
    ///     Gets the element source type name for collections, if applicable.
    /// </summary>
    public string? ElementSourceTypeName { get; }

    /// <summary>
    ///     Gets a value indicating whether this property has a default value.
    /// </summary>
    public bool HasDefaultValue { get; }

    /// <summary>
    ///     Gets a value indicating whether this is a collection type.
    /// </summary>
    public bool IsCollection { get; }

    /// <summary>
    ///     Gets a value indicating whether this is a framework type.
    /// </summary>
    public bool IsFrameworkType { get; }

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
    ///     Determines whether a property has a default value.
    /// </summary>
    /// <param name="propertySymbol">The property symbol.</param>
    /// <returns><c>true</c> if the property has a default value.</returns>
    private static bool HasPropertyDefaultValue(
        IPropertySymbol propertySymbol
    )
    {
        // Check for initializer in declaring syntax
        // This is a simplified check; full implementation would need syntax analysis
        foreach (SyntaxReference syntaxRef in propertySymbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = syntaxRef.GetSyntax();
            string syntaxText = syntax.ToString();

            // Look for "= ..." pattern (property initializer)
            if (syntaxText.Contains(" = ") || syntaxText.Contains("= "))
            {
                return true;
            }
        }

        return false;
    }
}