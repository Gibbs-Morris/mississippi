using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Mississippi.Inlet.Generators.Core.Analysis;

using NSubstitute;


namespace Mississippi.Inlet.Generators.Core.L0Tests.Analysis;

/// <summary>
///     Tests for <see cref="Core.Analysis.PropertyModel" />.
/// </summary>
public class PropertyModelTests
{
    /// <summary>
    ///     Creates a mock collection type.
    /// </summary>
    private static INamedTypeSymbol CreateCollectionType(
        string collectionName,
        ITypeSymbol elementType
    )
    {
        // Capture elementType.Name immediately to avoid any lazy evaluation issues
        string elementTypeName = elementType.Name;
        INamedTypeSymbol namedType = Substitute.For<INamedTypeSymbol>();
        INamedTypeSymbol constructedFrom = Substitute.For<INamedTypeSymbol>();
        namedType.IsGenericType.Returns(true);
        constructedFrom.Name.Returns(collectionName);
        constructedFrom.ToDisplayString().Returns($"System.Collections.Generic.{collectionName}<T>");
        namedType.ConstructedFrom.Returns(constructedFrom);
        namedType.TypeArguments.Returns(ImmutableArray.Create(elementType));
        namedType.SpecialType.Returns(SpecialType.None);
        namedType.NullableAnnotation.Returns(NullableAnnotation.NotAnnotated);
        namedType.IsValueType.Returns(false);
        namedType.OriginalDefinition.Returns(namedType);
        namedType.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns($"{collectionName}<{elementTypeName}>");

        // For IsFrameworkType check - collections from System namespace are framework types
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("System.Collections.Generic");
        namedType.ContainingNamespace.Returns(namespaceSymbol);
        return namedType;
    }

    /// <summary>
    ///     Creates a custom type symbol.
    /// </summary>
    private static ITypeSymbol CreateCustomType(
        string name
    )
    {
        // Use ITypeSymbol directly (not INamedTypeSymbol) to avoid pattern matching issues
        // where the code checks "typeSymbol is INamedTypeSymbol { IsGenericType: true }"
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.SpecialType.Returns(SpecialType.None);
        typeSymbol.NullableAnnotation.Returns(NullableAnnotation.NotAnnotated);
        typeSymbol.IsValueType.Returns(false);
        typeSymbol.OriginalDefinition.Returns(typeSymbol);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        typeSymbol.ToDisplayString().Returns(name);
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat>()).Returns(name);
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns(name);
        return typeSymbol;
    }

    /// <summary>
    ///     Creates a framework type symbol.
    /// </summary>
    private static ITypeSymbol CreateFrameworkType(
        string name = "Int32",
        SpecialType specialType = SpecialType.System_Int32
    )
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.SpecialType.Returns(specialType);
        typeSymbol.NullableAnnotation.Returns(NullableAnnotation.NotAnnotated);
        typeSymbol.IsValueType.Returns(specialType != SpecialType.System_String);
        typeSymbol.OriginalDefinition.Returns(typeSymbol);
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns(name);
        return typeSymbol;
    }

    /// <summary>
    ///     Creates a mock property symbol.
    /// </summary>
    private static IPropertySymbol CreatePropertySymbol(
        string name,
        ITypeSymbol type
    )
    {
        IPropertySymbol propertySymbol = Substitute.For<IPropertySymbol>();
        propertySymbol.Name.Returns(name);
        propertySymbol.Type.Returns(type);
        propertySymbol.DeclaringSyntaxReferences.Returns(ImmutableArray<SyntaxReference>.Empty);
        propertySymbol.ContainingType.Returns((INamedTypeSymbol?)null);
        return propertySymbol;
    }

    /// <summary>
    ///     Constructor should throw for null property symbol.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullPropertySymbol()
    {
        Assert.Throws<ArgumentNullException>(() => new PropertyModel(null!));
    }

    /// <summary>
    ///     Default suffix should be Dto.
    /// </summary>
    [Fact]
    public void DefaultSuffixIsDto()
    {
        ITypeSymbol customType = CreateCustomType("Product");
        IPropertySymbol propertySymbol = CreatePropertySymbol("Product", customType);
        PropertyModel model = new(propertySymbol);
        Assert.Equal("ProductDto", model.DtoTypeName);
    }

    /// <summary>
    ///     DtoTypeName should add suffix for custom types.
    /// </summary>
    [Fact]
    public void DtoTypeNameAddsSuffixForCustomType()
    {
        ITypeSymbol customType = CreateCustomType("Address");
        IPropertySymbol propertySymbol = CreatePropertySymbol("BillingAddress", customType);
        PropertyModel model = new(propertySymbol);
        Assert.Equal("AddressDto", model.DtoTypeName);
    }

    /// <summary>
    ///     DtoTypeName should return framework type as-is.
    /// </summary>
    [Fact]
    public void DtoTypeNameReturnsFrameworkTypeAsIs()
    {
        ITypeSymbol stringType = CreateFrameworkType("String", SpecialType.System_String);
        IPropertySymbol propertySymbol = CreatePropertySymbol("Name", stringType);
        PropertyModel model = new(propertySymbol);
        Assert.Equal("String", model.DtoTypeName);
    }

    /// <summary>
    ///     ElementDtoTypeName should be populated for framework collection with custom element type.
    /// </summary>
    /// <remarks>
    ///     The implementation populates element info when the element type is a custom type,
    ///     regardless of whether the collection itself is a framework type. This enables proper
    ///     DTO generation for collections like List&lt;CustomType&gt;.
    /// </remarks>
    [Fact]
    public void ElementDtoTypeNamePopulatedForFrameworkCollectionWithCustomElementType()
    {
        ITypeSymbol customType = CreateCustomType("LineItem");
        INamedTypeSymbol listType = CreateCollectionType("List", customType);
        IPropertySymbol propertySymbol = CreatePropertySymbol("Items", listType);
        PropertyModel model = new(propertySymbol);
        Assert.NotNull(model.ElementDtoTypeName);
        Assert.Equal("LineItemDto", model.ElementDtoTypeName);
    }

    /// <summary>
    ///     ElementSourceTypeName should be null for non-collection.
    /// </summary>
    [Fact]
    public void ElementSourceTypeNameNullForNonCollection()
    {
        ITypeSymbol intType = CreateFrameworkType();
        IPropertySymbol propertySymbol = CreatePropertySymbol("Count", intType);
        PropertyModel model = new(propertySymbol);
        Assert.Null(model.ElementSourceTypeName);
    }

    /// <summary>
    ///     ElementSourceTypeName should be populated for framework collection with custom element type.
    /// </summary>
    /// <remarks>
    ///     The implementation populates element info when the element type is a custom type,
    ///     regardless of whether the collection itself is a framework type. This enables proper
    ///     mapper generation for collections like List&lt;CustomType&gt;.
    /// </remarks>
    [Fact]
    public void ElementSourceTypeNamePopulatedForFrameworkCollectionWithCustomElementType()
    {
        ITypeSymbol customType = CreateCustomType("LineItem");
        customType.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns("LineItem");
        INamedTypeSymbol listType = CreateCollectionType("List", customType);
        IPropertySymbol propertySymbol = CreatePropertySymbol("Items", listType);
        PropertyModel model = new(propertySymbol);
        Assert.NotNull(model.ElementSourceTypeName);
        Assert.Equal("LineItem", model.ElementSourceTypeName);
    }

    /// <summary>
    ///     IsCollection should return false for non-collection types.
    /// </summary>
    [Fact]
    public void IsCollectionReturnsFalseForNonCollectionType()
    {
        ITypeSymbol stringType = CreateFrameworkType("String", SpecialType.System_String);
        IPropertySymbol propertySymbol = CreatePropertySymbol("Name", stringType);
        PropertyModel model = new(propertySymbol);
        Assert.False(model.IsCollection);
    }

    /// <summary>
    ///     IsCollection should return true for collection types.
    /// </summary>
    [Fact]
    public void IsCollectionReturnsTrueForCollectionType()
    {
        INamedTypeSymbol listType = CreateCollectionType("List", CreateFrameworkType());
        IPropertySymbol propertySymbol = CreatePropertySymbol("Items", listType);
        PropertyModel model = new(propertySymbol);
        Assert.True(model.IsCollection);
    }

    /// <summary>
    ///     IsFrameworkType should return false for custom types.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsFalseForCustomType()
    {
        ITypeSymbol customType = CreateCustomType("Order");
        IPropertySymbol propertySymbol = CreatePropertySymbol("CurrentOrder", customType);
        PropertyModel model = new(propertySymbol);
        Assert.False(model.IsFrameworkType);
    }

    /// <summary>
    ///     IsFrameworkType should return true for framework types.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsTrueForFrameworkType()
    {
        ITypeSymbol intType = CreateFrameworkType();
        IPropertySymbol propertySymbol = CreatePropertySymbol("Count", intType);
        PropertyModel model = new(propertySymbol);
        Assert.True(model.IsFrameworkType);
    }

    /// <summary>
    ///     IsNullable should return false for non-nullable types.
    /// </summary>
    [Fact]
    public void IsNullableReturnsFalseForNonNullable()
    {
        ITypeSymbol intType = CreateFrameworkType();
        intType.NullableAnnotation.Returns(NullableAnnotation.NotAnnotated);
        IPropertySymbol propertySymbol = CreatePropertySymbol("Count", intType);
        PropertyModel model = new(propertySymbol);
        Assert.False(model.IsNullable);
    }

    /// <summary>
    ///     IsNullable should return true for annotated nullable reference types.
    /// </summary>
    [Fact]
    public void IsNullableReturnsTrueForAnnotatedNullable()
    {
        ITypeSymbol stringType = CreateFrameworkType("String", SpecialType.System_String);
        stringType.NullableAnnotation.Returns(NullableAnnotation.Annotated);
        IPropertySymbol propertySymbol = CreatePropertySymbol("MiddleName", stringType);
        PropertyModel model = new(propertySymbol);
        Assert.True(model.IsNullable);
    }

    /// <summary>
    ///     IsNullable should return true for nullable value types.
    /// </summary>
    [Fact]
    public void IsNullableReturnsTrueForNullableValueType()
    {
        ITypeSymbol nullableInt = Substitute.For<ITypeSymbol>();
        nullableInt.IsValueType.Returns(true);
        nullableInt.SpecialType.Returns(SpecialType.System_Int32);
        ITypeSymbol originalDef = Substitute.For<ITypeSymbol>();
        originalDef.SpecialType.Returns(SpecialType.System_Nullable_T);
        nullableInt.OriginalDefinition.Returns(originalDef);
        nullableInt.NullableAnnotation.Returns(NullableAnnotation.None);
        nullableInt.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns("int?");
        IPropertySymbol propertySymbol = CreatePropertySymbol("OptionalCount", nullableInt);
        PropertyModel model = new(propertySymbol);
        Assert.True(model.IsNullable);
    }

    /// <summary>
    ///     IsRequired should return false when nullable.
    /// </summary>
    [Fact]
    public void IsRequiredReturnsFalseWhenNullable()
    {
        ITypeSymbol stringType = CreateFrameworkType("String", SpecialType.System_String);
        stringType.NullableAnnotation.Returns(NullableAnnotation.Annotated);
        IPropertySymbol propertySymbol = CreatePropertySymbol("OptionalField", stringType);
        PropertyModel model = new(propertySymbol);
        Assert.False(model.IsRequired);
    }

    /// <summary>
    ///     IsRequired should return true when not nullable and no default.
    /// </summary>
    [Fact]
    public void IsRequiredReturnsTrueWhenNotNullableAndNoDefault()
    {
        ITypeSymbol intType = CreateFrameworkType();
        intType.NullableAnnotation.Returns(NullableAnnotation.NotAnnotated);
        IPropertySymbol propertySymbol = CreatePropertySymbol("MandatoryField", intType);
        propertySymbol.DeclaringSyntaxReferences.Returns(ImmutableArray<SyntaxReference>.Empty);
        PropertyModel model = new(propertySymbol);
        Assert.True(model.IsRequired);
    }

    /// <summary>
    ///     Name should return property name.
    /// </summary>
    [Fact]
    public void NameReturnsPropertyName()
    {
        IPropertySymbol propertySymbol = CreatePropertySymbol("CustomerId", CreateFrameworkType());
        PropertyModel model = new(propertySymbol);
        Assert.Equal("CustomerId", model.Name);
    }

    /// <summary>
    ///     RequiresEnumerableMapper should return false for collection of framework types.
    /// </summary>
    [Fact]
    public void RequiresEnumerableMapperReturnsFalseForCollectionOfFrameworkType()
    {
        ITypeSymbol stringType = CreateFrameworkType("String", SpecialType.System_String);
        INamedTypeSymbol listType = CreateCollectionType("List", stringType);
        IPropertySymbol propertySymbol = CreatePropertySymbol("Tags", listType);
        PropertyModel model = new(propertySymbol);
        Assert.False(model.RequiresEnumerableMapper);
    }

    /// <summary>
    ///     RequiresEnumerableMapper should return true for collection of custom types.
    /// </summary>
    [Fact]
    public void RequiresEnumerableMapperReturnsTrueForCollectionOfCustomType()
    {
        ITypeSymbol customType = CreateCustomType("OrderItem");
        INamedTypeSymbol listType = CreateCollectionType("List", customType);
        IPropertySymbol propertySymbol = CreatePropertySymbol("LineItems", listType);
        PropertyModel model = new(propertySymbol);
        Assert.True(model.RequiresEnumerableMapper);
    }

    /// <summary>
    ///     RequiresMapper should return false for framework types.
    /// </summary>
    [Fact]
    public void RequiresMapperReturnsFalseForFrameworkType()
    {
        ITypeSymbol intType = CreateFrameworkType();
        IPropertySymbol propertySymbol = CreatePropertySymbol("Age", intType);
        PropertyModel model = new(propertySymbol);
        Assert.False(model.RequiresMapper);
    }

    /// <summary>
    ///     RequiresMapper should return true for custom types.
    /// </summary>
    [Fact]
    public void RequiresMapperReturnsTrueForCustomType()
    {
        ITypeSymbol customType = CreateCustomType("Customer");
        IPropertySymbol propertySymbol = CreatePropertySymbol("Customer", customType);
        PropertyModel model = new(propertySymbol);
        Assert.True(model.RequiresMapper);
    }

    /// <summary>
    ///     SourceTypeName should return minimally qualified type name.
    /// </summary>
    [Fact]
    public void SourceTypeNameReturnsMinimallyQualifiedName()
    {
        ITypeSymbol typeSymbol = CreateFrameworkType();
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns("Int32");
        IPropertySymbol propertySymbol = CreatePropertySymbol("Value", typeSymbol);
        PropertyModel model = new(propertySymbol);
        Assert.Equal("Int32", model.SourceTypeName);
    }
}