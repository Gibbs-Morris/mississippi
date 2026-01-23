using System;
using System.Collections.Immutable;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;

using Mississippi.Sdk.Generators.Core.Analysis;

using NSubstitute;


namespace Mississippi.Sdk.Generators.Core.L0Tests.Analysis;

/// <summary>
///     Tests for <see cref="Core.Analysis.CommandModel" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Generators Core")]
[AllureSubSuite("Command Model")]
public class CommandModelTests
{
    /// <summary>
    ///     Creates a mock command type symbol.
    /// </summary>
    private static INamedTypeSymbol CreateCommandTypeSymbol(
        string name,
        bool isRecord = false,
        int constructorParamCount = 0
    )
    {
        INamedTypeSymbol typeSymbol = Substitute.For<INamedTypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.ToDisplayString().Returns($"MyApp.Domain.Commands.{name}");
        typeSymbol.SpecialType.Returns(SpecialType.None);
        typeSymbol.IsRecord.Returns(isRecord);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain.Commands");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        typeSymbol.GetMembers().Returns(ImmutableArray<ISymbol>.Empty);
        IMethodSymbol constructor = CreateConstructor(constructorParamCount);
        typeSymbol.Constructors.Returns(ImmutableArray.Create(constructor));
        return typeSymbol;
    }

    /// <summary>
    ///     Creates a mock command type with specific members.
    /// </summary>
    private static INamedTypeSymbol CreateCommandTypeSymbolWithMembers(
        string name,
        params IPropertySymbol[] properties
    )
    {
        INamedTypeSymbol typeSymbol = Substitute.For<INamedTypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.ToDisplayString().Returns($"MyApp.Domain.Commands.{name}");
        typeSymbol.SpecialType.Returns(SpecialType.None);
        typeSymbol.IsRecord.Returns(false);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain.Commands");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        typeSymbol.GetMembers().Returns(ImmutableArray.Create(properties.Cast<ISymbol>().ToArray()));
        IMethodSymbol constructor = CreateConstructor(0);
        typeSymbol.Constructors.Returns(ImmutableArray.Create(constructor));
        return typeSymbol;
    }

    /// <summary>
    ///     Creates a mock command type with properties.
    /// </summary>
    private static INamedTypeSymbol CreateCommandTypeSymbolWithProperties(
        string name,
        params string[] propertyNames
    )
    {
        INamedTypeSymbol typeSymbol = Substitute.For<INamedTypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.ToDisplayString().Returns($"MyApp.Domain.Commands.{name}");
        typeSymbol.SpecialType.Returns(SpecialType.None);
        typeSymbol.IsRecord.Returns(false);
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns("MyApp.Domain.Commands");
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        ISymbol[] properties = propertyNames.Select(n => (ISymbol)CreatePropertySymbol(n)).ToArray();
        typeSymbol.GetMembers().Returns(ImmutableArray.Create(properties));
        IMethodSymbol constructor = CreateConstructor(0);
        typeSymbol.Constructors.Returns(ImmutableArray.Create(constructor));
        return typeSymbol;
    }

    /// <summary>
    ///     Creates a mock constructor symbol.
    /// </summary>
    private static IMethodSymbol CreateConstructor(
        int parameterCount
    )
    {
        IMethodSymbol constructor = Substitute.For<IMethodSymbol>();
        constructor.IsStatic.Returns(false);
        IParameterSymbol[] parameters = Enumerable.Range(0, parameterCount)
            .Select(_ => Substitute.For<IParameterSymbol>())
            .ToArray();
        constructor.Parameters.Returns(ImmutableArray.Create(parameters));
        return constructor;
    }

    /// <summary>
    ///     Creates a mock property symbol.
    /// </summary>
    private static IPropertySymbol CreatePropertySymbol(
        string name,
        bool isStatic = false,
        bool hasGetter = true
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
        propertySymbol.IsStatic.Returns(isStatic);
        propertySymbol.GetMethod.Returns(hasGetter ? Substitute.For<IMethodSymbol>() : null);
        propertySymbol.DeclaringSyntaxReferences.Returns(ImmutableArray<SyntaxReference>.Empty);
        propertySymbol.ContainingType.Returns((INamedTypeSymbol?)null);
        return propertySymbol;
    }

    /// <summary>
    ///     Constructor should throw for null HTTP method.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullHttpMethod()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateCustomer");
        Assert.Throws<ArgumentNullException>(() => new CommandModel(typeSymbol, "create", null!));
    }

    /// <summary>
    ///     Constructor should throw for null route.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullRoute()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateCustomer");
        Assert.Throws<ArgumentNullException>(() => new CommandModel(typeSymbol, null!, "POST"));
    }

    /// <summary>
    ///     Constructor should throw for null type symbol.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullTypeSymbol()
    {
        Assert.Throws<ArgumentNullException>(() => new CommandModel(null!, "create", "POST"));
    }

    /// <summary>
    ///     DtoTypeName should derive from type name with suffix.
    /// </summary>
    [Fact]
    public void DtoTypeNameDerivesFromTypeName()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder");
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.Equal("CreateOrderDto", model.DtoTypeName);
    }

    /// <summary>
    ///     FullTypeName should return fully qualified name.
    /// </summary>
    [Fact]
    public void FullTypeNameReturnsFullyQualifiedName()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder");
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.Equal("MyApp.Domain.Commands.CreateOrder", model.FullTypeName);
    }

    /// <summary>
    ///     HttpMethod should return provided value.
    /// </summary>
    [Fact]
    public void HttpMethodReturnsProvidedValue()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("UpdateOrder");
        CommandModel model = new(typeSymbol, "update", "PUT");
        Assert.Equal("PUT", model.HttpMethod);
    }

    /// <summary>
    ///     IsPositionalRecord should return false for non-record.
    /// </summary>
    [Fact]
    public void IsPositionalRecordReturnsFalseForNonRecord()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder");
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.False(model.IsPositionalRecord);
    }

    /// <summary>
    ///     IsPositionalRecord should return false for record without primary constructor params.
    /// </summary>
    [Fact]
    public void IsPositionalRecordReturnsFalseForRecordWithoutParams()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder", true);
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.False(model.IsPositionalRecord);
    }

    /// <summary>
    ///     IsPositionalRecord should return true for record with primary constructor.
    /// </summary>
    [Fact]
    public void IsPositionalRecordReturnsTrueForRecordWithConstructor()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder", true, 2);
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.True(model.IsPositionalRecord);
    }

    /// <summary>
    ///     Namespace should return containing namespace.
    /// </summary>
    [Fact]
    public void NamespaceReturnsContainingNamespace()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder");
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.Equal("MyApp.Domain.Commands", model.Namespace);
    }

    /// <summary>
    ///     PositionalConstructorParameterCount should return count for positional record.
    /// </summary>
    [Fact]
    public void PositionalConstructorParameterCountReturnsCountForPositional()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder", true, 3);
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.Equal(3, model.PositionalConstructorParameterCount);
    }

    /// <summary>
    ///     PositionalConstructorParameterCount should return zero for non-positional.
    /// </summary>
    [Fact]
    public void PositionalConstructorParameterCountZeroForNonPositional()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder");
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.Equal(0, model.PositionalConstructorParameterCount);
    }

    /// <summary>
    ///     Properties should exclude properties without getters.
    /// </summary>
    [Fact]
    public void PropertiesExcludesPropertiesWithoutGetter()
    {
        IPropertySymbol withGetter = CreatePropertySymbol("WithGetter", hasGetter: true);
        IPropertySymbol withoutGetter = CreatePropertySymbol("WithoutGetter", hasGetter: false);
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbolWithMembers("CreateOrder", withGetter, withoutGetter);
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.Single(model.Properties);
        Assert.Equal("WithGetter", model.Properties[0].Name);
    }

    /// <summary>
    ///     Properties should exclude static properties.
    /// </summary>
    [Fact]
    public void PropertiesExcludesStaticProperties()
    {
        IPropertySymbol instanceProp = CreatePropertySymbol("Instance");
        IPropertySymbol staticProp = CreatePropertySymbol("Static", true);
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbolWithMembers("CreateOrder", instanceProp, staticProp);
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.Single(model.Properties);
        Assert.Equal("Instance", model.Properties[0].Name);
    }

    /// <summary>
    ///     Properties should be extracted from type members.
    /// </summary>
    [Fact]
    public void PropertiesExtractedFromTypeMembers()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbolWithProperties("CreateOrder", "Name", "Amount");
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.Equal(2, model.Properties.Length);
        Assert.Contains(model.Properties, p => p.Name == "Name");
        Assert.Contains(model.Properties, p => p.Name == "Amount");
    }

    /// <summary>
    ///     Route should return provided value.
    /// </summary>
    [Fact]
    public void RouteReturnsProvidedValue()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder");
        CommandModel model = new(typeSymbol, "orders/create", "POST");
        Assert.Equal("orders/create", model.Route);
    }

    /// <summary>
    ///     TypeName should return the short name.
    /// </summary>
    [Fact]
    public void TypeNameReturnsShortName()
    {
        INamedTypeSymbol typeSymbol = CreateCommandTypeSymbol("CreateOrder");
        CommandModel model = new(typeSymbol, "create", "POST");
        Assert.Equal("CreateOrder", model.TypeName);
    }
}