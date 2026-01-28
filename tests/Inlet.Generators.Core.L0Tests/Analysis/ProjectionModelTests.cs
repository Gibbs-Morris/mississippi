using System;
using System.Collections.Immutable;
using System.Linq;


using Microsoft.CodeAnalysis;

using Mississippi.Inlet.Generators.Core.Analysis;

using NSubstitute;


namespace Mississippi.Inlet.Generators.Core.L0Tests.Analysis;

/// <summary>
///     Tests for <see cref="Core.Analysis.ProjectionModel" />.
/// </summary>
public class ProjectionModelTests
{
    /// <summary>
    ///     Creates a mock property symbol for a custom type.
    /// </summary>
    private static IPropertySymbol CreateCustomTypeProperty(
        string typeName,
        string propertyName = "CustomProperty"
    )
    {
        IPropertySymbol propertySymbol = Substitute.For<IPropertySymbol>();
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(typeName);
        typeSymbol.SpecialType.Returns(SpecialType.None);
        typeSymbol.NullableAnnotation.Returns(NullableAnnotation.NotAnnotated);
        typeSymbol.IsValueType.Returns(false);
        typeSymbol.OriginalDefinition.Returns(typeSymbol);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain.ValueObjects");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns(typeName);
        propertySymbol.Name.Returns(propertyName);
        propertySymbol.Type.Returns(typeSymbol);
        propertySymbol.DeclaredAccessibility.Returns(Accessibility.Public);
        propertySymbol.IsStatic.Returns(false);
        propertySymbol.GetMethod.Returns(Substitute.For<IMethodSymbol>());
        propertySymbol.DeclaringSyntaxReferences.Returns(ImmutableArray<SyntaxReference>.Empty);
        propertySymbol.ContainingType.Returns((INamedTypeSymbol?)null);
        return propertySymbol;
    }

    /// <summary>
    ///     Creates a mock property symbol for a framework type.
    /// </summary>
    private static IPropertySymbol CreateFrameworkTypeProperty(
        string name
    )
    {
        IPropertySymbol propertySymbol = Substitute.For<IPropertySymbol>();
        ITypeSymbol typeSymbol = Substitute.For<ITypeSymbol>();
        typeSymbol.SpecialType.Returns(SpecialType.System_String);
        typeSymbol.NullableAnnotation.Returns(NullableAnnotation.NotAnnotated);
        typeSymbol.IsValueType.Returns(false);
        typeSymbol.OriginalDefinition.Returns(typeSymbol);
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat?>()).Returns("String");
        propertySymbol.Name.Returns(name);
        propertySymbol.Type.Returns(typeSymbol);
        propertySymbol.DeclaredAccessibility.Returns(Accessibility.Public);
        propertySymbol.IsStatic.Returns(false);
        propertySymbol.GetMethod.Returns(Substitute.For<IMethodSymbol>());
        propertySymbol.DeclaringSyntaxReferences.Returns(ImmutableArray<SyntaxReference>.Empty);
        propertySymbol.ContainingType.Returns((INamedTypeSymbol?)null);
        return propertySymbol;
    }

    /// <summary>
    ///     Creates a mock projection type symbol.
    /// </summary>
    private static INamedTypeSymbol CreateProjectionTypeSymbol(
        string name
    )
    {
        INamedTypeSymbol typeSymbol = Substitute.For<INamedTypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.ToDisplayString().Returns($"MyApp.Domain.Projections.{name}");
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat>()).Returns($"global::MyApp.Domain.Projections.{name}");
        typeSymbol.SpecialType.Returns(SpecialType.None);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain.Projections");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        typeSymbol.GetMembers().Returns(ImmutableArray<ISymbol>.Empty);
        return typeSymbol;
    }

    /// <summary>
    ///     Creates a mock projection type with specific members.
    /// </summary>
    private static INamedTypeSymbol CreateProjectionTypeSymbolWithMembers(
        string name,
        params IPropertySymbol[] properties
    )
    {
        INamedTypeSymbol typeSymbol = Substitute.For<INamedTypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.ToDisplayString().Returns($"MyApp.Domain.Projections.{name}");
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat>()).Returns($"global::MyApp.Domain.Projections.{name}");
        typeSymbol.SpecialType.Returns(SpecialType.None);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain.Projections");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        typeSymbol.GetMembers().Returns(ImmutableArray.Create(properties.Cast<ISymbol>().ToArray()));
        return typeSymbol;
    }

    /// <summary>
    ///     Creates a mock projection type with properties.
    /// </summary>
    private static INamedTypeSymbol CreateProjectionTypeSymbolWithProperties(
        string name,
        params string[] propertyNames
    )
    {
        INamedTypeSymbol typeSymbol = Substitute.For<INamedTypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.ToDisplayString().Returns($"MyApp.Domain.Projections.{name}");
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat>()).Returns($"global::MyApp.Domain.Projections.{name}");
        typeSymbol.SpecialType.Returns(SpecialType.None);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain.Projections");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        ISymbol[] properties = propertyNames.Select(n => (ISymbol)CreateFrameworkTypeProperty(n)).ToArray();
        typeSymbol.GetMembers().Returns(ImmutableArray.Create(properties));
        return typeSymbol;
    }

    /// <summary>
    ///     Constructor should throw for null projection path.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullProjectionPath()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbol("CustomerProjection");
        Assert.Throws<ArgumentNullException>(() => new ProjectionModel(typeSymbol, null!));
    }

    /// <summary>
    ///     Constructor should throw for null type symbol.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullTypeSymbol()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectionModel(null!));
    }

    /// <summary>
    ///     Constructor with path should throw for null type symbol.
    /// </summary>
    [Fact]
    public void ConstructorWithPathThrowsForNullTypeSymbol()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectionModel(null!, "/customers"));
    }

    /// <summary>
    ///     Default constructor should set empty projection path.
    /// </summary>
    [Fact]
    public void DefaultConstructorSetsEmptyProjectionPath()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbol("CustomerProjection");
        ProjectionModel model = new(typeSymbol);
        Assert.Equal(string.Empty, model.ProjectionPath);
    }

    /// <summary>
    ///     DtoTypeName should derive from type name with suffix.
    /// </summary>
    [Fact]
    public void DtoTypeNameDerivesFromTypeName()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbol("OrderProjection");
        ProjectionModel model = new(typeSymbol, "/orders");
        Assert.Equal("OrderDto", model.DtoTypeName);
    }

    /// <summary>
    ///     FullTypeName should return fully qualified name with global:: prefix.
    /// </summary>
    [Fact]
    public void FullTypeNameReturnsFullyQualifiedName()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbol("OrderProjection");
        ProjectionModel model = new(typeSymbol, "/orders");
        Assert.Equal("global::MyApp.Domain.Projections.OrderProjection", model.FullTypeName);
    }

    /// <summary>
    ///     HasMappedProperties should return false when only framework type properties exist.
    /// </summary>
    [Fact]
    public void HasMappedPropertiesReturnsFalseForFrameworkTypeProperties()
    {
        IPropertySymbol stringProp = CreateFrameworkTypeProperty("Name");
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbolWithMembers("CustomerProjection", stringProp);
        ProjectionModel model = new(typeSymbol, "/customers");
        Assert.False(model.HasMappedProperties);
    }

    /// <summary>
    ///     HasMappedProperties should return true when custom type properties exist.
    /// </summary>
    [Fact]
    public void HasMappedPropertiesReturnsTrueForCustomTypeProperties()
    {
        IPropertySymbol customProp = CreateCustomTypeProperty("Address");
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbolWithMembers("CustomerProjection", customProp);
        ProjectionModel model = new(typeSymbol, "/customers");
        Assert.True(model.HasMappedProperties);
    }

    /// <summary>
    ///     Namespace should return containing namespace.
    /// </summary>
    [Fact]
    public void NamespaceReturnsContainingNamespace()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbol("OrderProjection");
        ProjectionModel model = new(typeSymbol, "/orders");
        Assert.Equal("MyApp.Domain.Projections", model.Namespace);
    }

    /// <summary>
    ///     NestedCustomTypes should contain custom type names.
    /// </summary>
    [Fact]
    public void NestedCustomTypesContainsCustomTypeNames()
    {
        IPropertySymbol customProp = CreateCustomTypeProperty("Address");
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbolWithMembers("CustomerProjection", customProp);
        ProjectionModel model = new(typeSymbol, "/customers");
        Assert.Contains("Address", model.NestedCustomTypes);
    }

    /// <summary>
    ///     NestedCustomTypes should be distinct.
    /// </summary>
    [Fact]
    public void NestedCustomTypesIsDistinct()
    {
        IPropertySymbol customProp1 = CreateCustomTypeProperty("Address", "BillingAddress");
        IPropertySymbol customProp2 = CreateCustomTypeProperty("Address", "ShippingAddress");
        INamedTypeSymbol typeSymbol =
            CreateProjectionTypeSymbolWithMembers("CustomerProjection", customProp1, customProp2);
        ProjectionModel model = new(typeSymbol, "/customers");
        Assert.Single(model.NestedCustomTypes);
        Assert.Equal("Address", model.NestedCustomTypes[0]);
    }

    /// <summary>
    ///     ProjectionName should remove Projection suffix.
    /// </summary>
    [Fact]
    public void ProjectionNameRemovesProjectionSuffix()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbol("CustomerProjection");
        ProjectionModel model = new(typeSymbol, "/customers");
        Assert.Equal("Customer", model.ProjectionName);
    }

    /// <summary>
    ///     ProjectionName should return type name if no Projection suffix.
    /// </summary>
    [Fact]
    public void ProjectionNameReturnsTypeNameIfNoSuffix()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbol("OrderView");
        ProjectionModel model = new(typeSymbol, "/orders");
        Assert.Equal("OrderView", model.ProjectionName);
    }

    /// <summary>
    ///     ProjectionPath should return provided value.
    /// </summary>
    [Fact]
    public void ProjectionPathReturnsProvidedValue()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbol("CustomerProjection");
        ProjectionModel model = new(typeSymbol, "/api/v1/customers");
        Assert.Equal("/api/v1/customers", model.ProjectionPath);
    }

    /// <summary>
    ///     Properties should be extracted from type members.
    /// </summary>
    [Fact]
    public void PropertiesExtractedFromTypeMembers()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbolWithProperties("CustomerProjection", "Name", "Email");
        ProjectionModel model = new(typeSymbol, "/customers");
        Assert.Equal(2, model.Properties.Length);
        Assert.Contains(model.Properties, p => p.Name == "Name");
        Assert.Contains(model.Properties, p => p.Name == "Email");
    }

    /// <summary>
    ///     TypeName should return the short name.
    /// </summary>
    [Fact]
    public void TypeNameReturnsShortName()
    {
        INamedTypeSymbol typeSymbol = CreateProjectionTypeSymbol("OrderProjection");
        ProjectionModel model = new(typeSymbol, "/orders");
        Assert.Equal("OrderProjection", model.TypeName);
    }
}