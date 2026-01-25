using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;

using Mississippi.Inlet.Generators.Core.Analysis;

using NSubstitute;


namespace Mississippi.Inlet.Generators.Core.L0Tests.Analysis;

/// <summary>
///     Tests for <see cref="TypeAnalyzer" /> type analysis utilities.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Generators Core")]
[AllureSubSuite("Type Analyzer")]
public class TypeAnalyzerTests
{
    /// <summary>
    ///     Creates a mock named type symbol representing a collection type.
    /// </summary>
    private static INamedTypeSymbol CreateCollectionType(
        string fullyQualifiedName
    )
    {
        INamedTypeSymbol namedType = Substitute.For<INamedTypeSymbol>();
        INamedTypeSymbol constructedFrom = Substitute.For<INamedTypeSymbol>();
        namedType.IsGenericType.Returns(true);
        constructedFrom.ToDisplayString().Returns(fullyQualifiedName);
        namedType.ConstructedFrom.Returns(constructedFrom);
        return namedType;
    }

    /// <summary>
    ///     Creates a mock type symbol representing a custom (non-framework) type.
    /// </summary>
    private static ITypeSymbol CreateCustomType(
        string name
    )
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.SpecialType.Returns(SpecialType.None);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns(name);
        return typeSymbol;
    }

    /// <summary>
    ///     Creates a mock type symbol representing a framework type.
    /// </summary>
    private static ITypeSymbol CreateFrameworkType(
        string name,
        SpecialType specialType
    )
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.SpecialType.Returns(specialType);
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns(name);
        return typeSymbol;
    }

    /// <summary>
    ///     Creates a mock type symbol in a specific namespace.
    /// </summary>
    private static ITypeSymbol CreateTypeWithNamespace(
        string name,
        string namespaceName
    )
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.SpecialType.Returns(SpecialType.None);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns(namespaceName);
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        return typeSymbol;
    }

    /// <summary>
    ///     GetBooleanProperty should return default value when attribute is null.
    /// </summary>
    [Fact]
    public void GetBooleanPropertyReturnsDefaultWhenAttributeIsNull()
    {
        bool result = TypeAnalyzer.GetBooleanProperty(null!, "SomeProperty");
        Assert.True(result);
        result = TypeAnalyzer.GetBooleanProperty(null!, "SomeProperty", false);
        Assert.False(result);
    }

    /// <summary>
    ///     GetBooleanProperty should return default value when property is not found.
    /// </summary>
    [Fact]
    public void GetBooleanPropertyReturnsDefaultWhenPropertyNotFound()
    {
        AttributeData attribute = Substitute.For<AttributeData>();
        attribute.NamedArguments.Returns(ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        bool result = TypeAnalyzer.GetBooleanProperty(attribute, "MissingProperty");
        Assert.True(result);
    }

    /// <summary>
    ///     GetCollectionElementType should return element type for array types.
    /// </summary>
    [Fact]
    public void GetCollectionElementTypeReturnsElementTypeForArray()
    {
        IArrayTypeSymbol arrayType = Substitute.For<IArrayTypeSymbol>();
        ITypeSymbol elementType = Substitute.For<ITypeSymbol>();
        arrayType.ElementType.Returns(elementType);
        ITypeSymbol? result = TypeAnalyzer.GetCollectionElementType(arrayType);
        Assert.Same(elementType, result);
    }

    /// <summary>
    ///     GetCollectionElementType should return first type argument for generic types.
    /// </summary>
    [Fact]
    public void GetCollectionElementTypeReturnsFirstTypeArgForGeneric()
    {
        INamedTypeSymbol namedType = Substitute.For<INamedTypeSymbol>();
        ITypeSymbol elementType = Substitute.For<ITypeSymbol>();
        namedType.IsGenericType.Returns(true);
        namedType.TypeArguments.Returns(ImmutableArray.Create(elementType));
        ITypeSymbol? result = TypeAnalyzer.GetCollectionElementType(namedType);
        Assert.Same(elementType, result);
    }

    /// <summary>
    ///     GetCollectionElementType should return null when generic type has no type arguments.
    /// </summary>
    [Fact]
    public void GetCollectionElementTypeReturnsNullForEmptyTypeArgs()
    {
        INamedTypeSymbol namedType = Substitute.For<INamedTypeSymbol>();
        namedType.IsGenericType.Returns(true);
        namedType.TypeArguments.Returns(ImmutableArray<ITypeSymbol>.Empty);
        ITypeSymbol? result = TypeAnalyzer.GetCollectionElementType(namedType);
        Assert.Null(result);
    }

    /// <summary>
    ///     GetCollectionElementType should return null for non-collection types.
    /// </summary>
    [Fact]
    public void GetCollectionElementTypeReturnsNullForNonCollection()
    {
        INamedTypeSymbol namedType = Substitute.For<INamedTypeSymbol>();
        namedType.IsGenericType.Returns(false);
        namedType.TypeArguments.Returns(ImmutableArray<ITypeSymbol>.Empty);
        ITypeSymbol? result = TypeAnalyzer.GetCollectionElementType(namedType);
        Assert.Null(result);
    }

    /// <summary>
    ///     GetDtoTypeName should add suffix to array element type.
    /// </summary>
    [Fact]
    public void GetDtoTypeNameAddsArraySuffixToElementDto()
    {
        IArrayTypeSymbol arrayType = Substitute.For<IArrayTypeSymbol>();
        ITypeSymbol elementType = CreateCustomType("Customer");
        arrayType.ElementType.Returns(elementType);
        string result = TypeAnalyzer.GetDtoTypeName(arrayType);
        Assert.Equal("CustomerDto[]", result);
    }

    /// <summary>
    ///     GetDtoTypeName should add custom suffix to plain custom type.
    /// </summary>
    [Fact]
    public void GetDtoTypeNameAddsCustomSuffixToPlainType()
    {
        ITypeSymbol typeSymbol = CreateCustomType("Address");
        string result = TypeAnalyzer.GetDtoTypeName(typeSymbol);
        Assert.Equal("AddressDto", result);
    }

    /// <summary>
    ///     GetDtoTypeName should map generic type arguments.
    /// </summary>
    [Fact]
    public void GetDtoTypeNameMapsGenericTypeArgs()
    {
        INamedTypeSymbol namedType = Substitute.For<INamedTypeSymbol>();
        INamedTypeSymbol constructedFrom = Substitute.For<INamedTypeSymbol>();
        ITypeSymbol elementType = CreateCustomType("Order");
        namedType.IsGenericType.Returns(true);
        constructedFrom.Name.Returns("List");
        namedType.ConstructedFrom.Returns(constructedFrom);
        namedType.TypeArguments.Returns(ImmutableArray.Create(elementType));
        string result = TypeAnalyzer.GetDtoTypeName(namedType);
        Assert.Equal("List<OrderDto>", result);
    }

    /// <summary>
    ///     GetDtoTypeName should remove Aggregate suffix and add custom suffix.
    /// </summary>
    [Fact]
    public void GetDtoTypeNameRemovesAggregateSuffix()
    {
        ITypeSymbol typeSymbol = CreateCustomType("CustomerAggregate");
        string result = TypeAnalyzer.GetDtoTypeName(typeSymbol);
        Assert.Equal("CustomerDto", result);
    }

    /// <summary>
    ///     GetDtoTypeName should remove Projection suffix and add custom suffix.
    /// </summary>
    [Fact]
    public void GetDtoTypeNameRemovesProjectionSuffix()
    {
        ITypeSymbol typeSymbol = CreateCustomType("CustomerProjection");
        string result = TypeAnalyzer.GetDtoTypeName(typeSymbol);
        Assert.Equal("CustomerDto", result);
    }

    /// <summary>
    ///     GetDtoTypeName should remove State suffix and add custom suffix.
    /// </summary>
    [Fact]
    public void GetDtoTypeNameRemovesStateSuffix()
    {
        ITypeSymbol typeSymbol = CreateCustomType("CustomerState");
        string result = TypeAnalyzer.GetDtoTypeName(typeSymbol);
        Assert.Equal("CustomerDto", result);
    }

    /// <summary>
    ///     GetDtoTypeName should return framework type as-is.
    /// </summary>
    [Fact]
    public void GetDtoTypeNameReturnsFrameworkTypeAsIs()
    {
        ITypeSymbol typeSymbol = CreateFrameworkType("String", SpecialType.System_String);
        string result = TypeAnalyzer.GetDtoTypeName(typeSymbol);
        Assert.Equal("String", result);
    }

    /// <summary>
    ///     GetDtoTypeName should throw for null type symbol.
    /// </summary>
    [Fact]
    public void GetDtoTypeNameThrowsForNullTypeSymbol()
    {
        Assert.Throws<ArgumentNullException>(() => TypeAnalyzer.GetDtoTypeName(null!));
    }

    /// <summary>
    ///     GetDtoTypeName should use default Dto suffix.
    /// </summary>
    [Fact]
    public void GetDtoTypeNameUsesDefaultDtoSuffix()
    {
        ITypeSymbol typeSymbol = CreateCustomType("Product");
        string result = TypeAnalyzer.GetDtoTypeName(typeSymbol);
        Assert.Equal("ProductDto", result);
    }

    /// <summary>
    ///     GetFullNamespace should return empty for global namespace.
    /// </summary>
    [Fact]
    public void GetFullNamespaceReturnsEmptyForGlobalNamespace()
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        namespaceSymbol.IsGlobalNamespace.Returns(true);
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        string result = TypeAnalyzer.GetFullNamespace(typeSymbol);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     GetFullNamespace should return empty for null containing namespace.
    /// </summary>
    [Fact]
    public void GetFullNamespaceReturnsEmptyForNullNamespace()
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        typeSymbol.ContainingNamespace.Returns((INamespaceSymbol?)null);
        string result = TypeAnalyzer.GetFullNamespace(typeSymbol);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     GetFullNamespace should return empty for null type symbol.
    /// </summary>
    [Fact]
    public void GetFullNamespaceReturnsEmptyForNullTypeSymbol()
    {
        string result = TypeAnalyzer.GetFullNamespace(null!);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     GetFullNamespace should return namespace display string.
    /// </summary>
    [Fact]
    public void GetFullNamespaceReturnsNamespaceDisplayString()
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain.Models");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        string result = TypeAnalyzer.GetFullNamespace(typeSymbol);
        Assert.Equal("MyApp.Domain.Models", result);
    }

    /// <summary>
    ///     GetNullableBooleanProperty should return null when attribute is null.
    /// </summary>
    [Fact]
    public void GetNullableBooleanPropertyReturnsNullWhenAttributeIsNull()
    {
        bool? result = TypeAnalyzer.GetNullableBooleanProperty(null, "SomeProperty");
        Assert.Null(result);
    }

    /// <summary>
    ///     GetNullableBooleanProperty should return null when property is not found.
    /// </summary>
    [Fact]
    public void GetNullableBooleanPropertyReturnsNullWhenPropertyNotFound()
    {
        AttributeData attribute = Substitute.For<AttributeData>();
        attribute.NamedArguments.Returns(ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        bool? result = TypeAnalyzer.GetNullableBooleanProperty(attribute, "MissingProperty");
        Assert.Null(result);
    }

    /// <summary>
    ///     GetStringProperty should return null when attribute is null.
    /// </summary>
    [Fact]
    public void GetStringPropertyReturnsNullWhenAttributeIsNull()
    {
        string? result = TypeAnalyzer.GetStringProperty(null, "SomeProperty");
        Assert.Null(result);
    }

    /// <summary>
    ///     GetStringProperty should return null when property is not found.
    /// </summary>
    [Fact]
    public void GetStringPropertyReturnsNullWhenPropertyNotFound()
    {
        AttributeData attribute = Substitute.For<AttributeData>();
        attribute.NamedArguments.Returns(ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        string? result = TypeAnalyzer.GetStringProperty(attribute, "MissingProperty");
        Assert.Null(result);
    }

    /// <summary>
    ///     IsCollectionType should return false for non-collection generic type.
    /// </summary>
    [Fact]
    public void IsCollectionTypeReturnsFalseForNonCollectionGeneric()
    {
        INamedTypeSymbol namedType = CreateCollectionType("System.Nullable<T>");
        bool result = TypeAnalyzer.IsCollectionType(namedType);
        Assert.False(result);
    }

    /// <summary>
    ///     IsCollectionType should return false for non-generic named types.
    /// </summary>
    [Fact]
    public void IsCollectionTypeReturnsFalseForNonGeneric()
    {
        INamedTypeSymbol namedType = Substitute.For<INamedTypeSymbol>();
        namedType.IsGenericType.Returns(false);
        bool result = TypeAnalyzer.IsCollectionType(namedType);
        Assert.False(result);
    }

    /// <summary>
    ///     IsCollectionType should return false for non-named types.
    /// </summary>
    [Fact]
    public void IsCollectionTypeReturnsFalseForNonNamedType()
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        bool result = TypeAnalyzer.IsCollectionType(typeSymbol);
        Assert.False(result);
    }

    /// <summary>
    ///     IsCollectionType should return false for pure array types (implementation quirk).
    /// </summary>
    /// <remarks>
    ///     Note: The implementation checks for INamedTypeSymbol first, so pure IArrayTypeSymbol
    ///     returns false. This test documents actual behavior.
    /// </remarks>
    [Fact]
    public void IsCollectionTypeReturnsFalseForPureArrayType()
    {
        IArrayTypeSymbol arrayType = Substitute.For<IArrayTypeSymbol>();
        bool result = TypeAnalyzer.IsCollectionType(arrayType);
        Assert.False(result);
    }

    /// <summary>
    ///     IsCollectionType should return true for System.Collections.Generic.List.
    /// </summary>
    [Fact]
    public void IsCollectionTypeReturnsTrueForGenericList()
    {
        INamedTypeSymbol namedType = CreateCollectionType("System.Collections.Generic.List<T>");
        bool result = TypeAnalyzer.IsCollectionType(namedType);
        Assert.True(result);
    }

    /// <summary>
    ///     IsCollectionType should return true for IEnumerable.
    /// </summary>
    [Fact]
    public void IsCollectionTypeReturnsTrueForIEnumerable()
    {
        INamedTypeSymbol namedType = CreateCollectionType("System.Collections.Generic.IEnumerable<T>");
        bool result = TypeAnalyzer.IsCollectionType(namedType);
        Assert.True(result);
    }

    /// <summary>
    ///     IsCollectionType should return true for ImmutableArray.
    /// </summary>
    [Fact]
    public void IsCollectionTypeReturnsTrueForImmutableArray()
    {
        INamedTypeSymbol namedType = CreateCollectionType("System.Collections.Immutable.ImmutableArray<T>");
        bool result = TypeAnalyzer.IsCollectionType(namedType);
        Assert.True(result);
    }

    /// <summary>
    ///     IsFrameworkType should return false for custom types in non-framework namespace.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsFalseForCustomNamespace()
    {
        ITypeSymbol typeSymbol = CreateTypeWithNamespace("Customer", "MyApp.Domain.Models");
        bool result = TypeAnalyzer.IsFrameworkType(typeSymbol);
        Assert.False(result);
    }

    /// <summary>
    ///     IsFrameworkType should return true for types in Microsoft namespace.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsTrueForMicrosoftNamespace()
    {
        ITypeSymbol typeSymbol = CreateTypeWithNamespace("Extensions", "Microsoft.Extensions");
        bool result = TypeAnalyzer.IsFrameworkType(typeSymbol);
        Assert.True(result);
    }

    /// <summary>
    ///     IsFrameworkType should return true for types in Newtonsoft namespace.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsTrueForNewtonsoftNamespace()
    {
        ITypeSymbol typeSymbol = CreateTypeWithNamespace("JToken", "Newtonsoft.Json.Linq");
        bool result = TypeAnalyzer.IsFrameworkType(typeSymbol);
        Assert.True(result);
    }

    /// <summary>
    ///     IsFrameworkType should return true for types with no namespace.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsTrueForNoNamespace()
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        typeSymbol.SpecialType.Returns(SpecialType.None);
        typeSymbol.ContainingNamespace.Returns((INamespaceSymbol?)null);
        bool result = TypeAnalyzer.IsFrameworkType(typeSymbol);
        Assert.True(result);
    }

    /// <summary>
    ///     IsFrameworkType should return true for null type symbol.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsTrueForNullType()
    {
        bool result = TypeAnalyzer.IsFrameworkType(null!);
        Assert.True(result);
    }

    /// <summary>
    ///     IsFrameworkType should return true for types in Orleans namespace.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsTrueForOrleansNamespace()
    {
        ITypeSymbol typeSymbol = CreateTypeWithNamespace("GrainId", "Orleans.Runtime");
        bool result = TypeAnalyzer.IsFrameworkType(typeSymbol);
        Assert.True(result);
    }

    /// <summary>
    ///     IsFrameworkType should return true for special types (primitives).
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsTrueForSpecialTypes()
    {
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        typeSymbol.SpecialType.Returns(SpecialType.System_Int32);
        bool result = TypeAnalyzer.IsFrameworkType(typeSymbol);
        Assert.True(result);
    }

    /// <summary>
    ///     IsFrameworkType should return true for types in System namespace.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsTrueForSystemNamespace()
    {
        ITypeSymbol typeSymbol = CreateTypeWithNamespace("Guid", "System");
        bool result = TypeAnalyzer.IsFrameworkType(typeSymbol);
        Assert.True(result);
    }

    /// <summary>
    ///     RequiresEnumerableMapper should return false for collection of framework types.
    /// </summary>
    [Fact]
    public void RequiresEnumerableMapperReturnsFalseForFrameworkElementType()
    {
        INamedTypeSymbol listType = Substitute.For<INamedTypeSymbol>();
        INamedTypeSymbol constructedFrom = Substitute.For<INamedTypeSymbol>();
        ITypeSymbol stringType = CreateFrameworkType("String", SpecialType.System_String);
        listType.IsGenericType.Returns(true);
        constructedFrom.ToDisplayString().Returns("System.Collections.Generic.List<T>");
        listType.ConstructedFrom.Returns(constructedFrom);
        listType.TypeArguments.Returns(ImmutableArray.Create(stringType));
        bool result = TypeAnalyzer.RequiresEnumerableMapper(listType);
        Assert.False(result);
    }

    /// <summary>
    ///     RequiresEnumerableMapper should return false for non-collection.
    /// </summary>
    [Fact]
    public void RequiresEnumerableMapperReturnsFalseForNonCollection()
    {
        ITypeSymbol typeSymbol = CreateCustomType("Customer");
        bool result = TypeAnalyzer.RequiresEnumerableMapper(typeSymbol);
        Assert.False(result);
    }

    /// <summary>
    ///     RequiresEnumerableMapper should return true for collection of custom types.
    /// </summary>
    [Fact]
    public void RequiresEnumerableMapperReturnsTrueForCustomElementType()
    {
        INamedTypeSymbol listType = Substitute.For<INamedTypeSymbol>();
        INamedTypeSymbol constructedFrom = Substitute.For<INamedTypeSymbol>();
        ITypeSymbol customType = CreateCustomType("OrderItem");
        listType.IsGenericType.Returns(true);
        constructedFrom.ToDisplayString().Returns("System.Collections.Generic.List<T>");
        listType.ConstructedFrom.Returns(constructedFrom);
        listType.TypeArguments.Returns(ImmutableArray.Create(customType));
        bool result = TypeAnalyzer.RequiresEnumerableMapper(listType);
        Assert.True(result);
    }

    /// <summary>
    ///     RequiresMapper should return false for collection with framework element type.
    /// </summary>
    [Fact]
    public void RequiresMapperReturnsFalseForCollectionOfFrameworkType()
    {
        INamedTypeSymbol listType = Substitute.For<INamedTypeSymbol>();
        INamedTypeSymbol constructedFrom = Substitute.For<INamedTypeSymbol>();
        ITypeSymbol stringType = CreateFrameworkType("String", SpecialType.System_String);
        listType.IsGenericType.Returns(true);
        constructedFrom.ToDisplayString().Returns("System.Collections.Generic.List<T>");
        listType.ConstructedFrom.Returns(constructedFrom);
        listType.TypeArguments.Returns(ImmutableArray.Create(stringType));
        bool result = TypeAnalyzer.RequiresMapper(listType);
        Assert.False(result);
    }

    /// <summary>
    ///     RequiresMapper should return false for framework types.
    /// </summary>
    [Fact]
    public void RequiresMapperReturnsFalseForFrameworkType()
    {
        ITypeSymbol typeSymbol = CreateFrameworkType("Int32", SpecialType.System_Int32);
        bool result = TypeAnalyzer.RequiresMapper(typeSymbol);
        Assert.False(result);
    }

    /// <summary>
    ///     RequiresMapper should return true for collection with custom element type.
    /// </summary>
    [Fact]
    public void RequiresMapperReturnsTrueForCollectionOfCustomType()
    {
        INamedTypeSymbol listType = Substitute.For<INamedTypeSymbol>();
        INamedTypeSymbol constructedFrom = Substitute.For<INamedTypeSymbol>();
        ITypeSymbol customType = CreateCustomType("Product");
        listType.IsGenericType.Returns(true);
        constructedFrom.ToDisplayString().Returns("System.Collections.Generic.List<T>");
        listType.ConstructedFrom.Returns(constructedFrom);
        listType.TypeArguments.Returns(ImmutableArray.Create(customType));
        bool result = TypeAnalyzer.RequiresMapper(listType);
        Assert.True(result);
    }

    /// <summary>
    ///     RequiresMapper should return true for custom types.
    /// </summary>
    [Fact]
    public void RequiresMapperReturnsTrueForCustomType()
    {
        ITypeSymbol typeSymbol = CreateCustomType("Customer");
        bool result = TypeAnalyzer.RequiresMapper(typeSymbol);
        Assert.True(result);
    }
}