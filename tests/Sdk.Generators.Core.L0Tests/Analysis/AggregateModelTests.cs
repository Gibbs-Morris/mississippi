using System;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;

using Mississippi.Sdk.Generators.Core.Analysis;

using NSubstitute;


namespace Mississippi.Sdk.Generators.Core.L0Tests.Analysis;

/// <summary>
///     Tests for <see cref="Core.Analysis.AggregateModel" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Generators Core")]
[AllureSubSuite("Aggregate Model")]
public class AggregateModelTests
{
    /// <summary>
    ///     Creates a mock INamedTypeSymbol with the given name and namespace.
    /// </summary>
    private static INamedTypeSymbol CreateNamedTypeSymbol(
        string name,
        string namespaceName
    )
    {
        INamedTypeSymbol typeSymbol = Substitute.For<INamedTypeSymbol>();
        INamespaceSymbol namespaceSymbol = Substitute.For<INamespaceSymbol>();
        typeSymbol.Name.Returns(name);
        typeSymbol.ToDisplayString().Returns($"{namespaceName}.{name}");
        typeSymbol.ToDisplayString(Arg.Any<SymbolDisplayFormat>()).Returns($"global::{namespaceName}.{name}");
        namespaceSymbol.IsGlobalNamespace.Returns(false);
        namespaceSymbol.ToDisplayString().Returns(namespaceName);
        typeSymbol.ContainingNamespace.Returns(namespaceSymbol);
        return typeSymbol;
    }

    /// <summary>
    ///     AggregateName should remove Aggregate suffix.
    /// </summary>
    [Fact]
    public void AggregateNameRemovesAggregateSuffix()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("CustomerAggregate", "MyApp.Domain");
        AggregateModel model = new(typeSymbol, "api/customers", "customers");
        Assert.Equal("Customer", model.AggregateName);
    }

    /// <summary>
    ///     AggregateName should return type name if no Aggregate suffix.
    /// </summary>
    [Fact]
    public void AggregateNameReturnsTypeNameIfNoSuffix()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("Order", "MyApp.Domain");
        AggregateModel model = new(typeSymbol, "api/orders", "orders");
        Assert.Equal("Order", model.AggregateName);
    }

    /// <summary>
    ///     Constructor should throw for null feature key.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullFeatureKey()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("CustomerAggregate", "MyApp.Domain");
        Assert.Throws<ArgumentNullException>(() => new AggregateModel(typeSymbol, "api/customers", null!));
    }

    /// <summary>
    ///     Constructor should throw for null route prefix.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullRoutePrefix()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("CustomerAggregate", "MyApp.Domain");
        Assert.Throws<ArgumentNullException>(() => new AggregateModel(typeSymbol, null!, "customers"));
    }

    /// <summary>
    ///     Constructor should throw for null type symbol.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullTypeSymbol()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateModel(null!, "api/customers", "customers"));
    }

    /// <summary>
    ///     ControllerTypeName should derive from aggregate name.
    /// </summary>
    [Fact]
    public void ControllerTypeNameDerivesFromAggregateName()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("CustomerAggregate", "MyApp.Domain");
        AggregateModel model = new(typeSymbol, "api/customers", "customers");
        Assert.Equal("CustomerController", model.ControllerTypeName);
    }

    /// <summary>
    ///     ControllerTypeName should use full type name if no suffix.
    /// </summary>
    [Fact]
    public void ControllerTypeNameUsesFullNameIfNoSuffix()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("Order", "MyApp.Domain");
        AggregateModel model = new(typeSymbol, "api/orders", "orders");
        Assert.Equal("OrderController", model.ControllerTypeName);
    }

    /// <summary>
    ///     FeatureKey should return provided value.
    /// </summary>
    [Fact]
    public void FeatureKeyReturnsProvidedValue()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("CustomerAggregate", "MyApp.Domain");
        AggregateModel model = new(typeSymbol, "api/customers", "customer-feature");
        Assert.Equal("customer-feature", model.FeatureKey);
    }

    /// <summary>
    ///     FullTypeName should return fully qualified name with global:: prefix.
    /// </summary>
    [Fact]
    public void FullTypeNameReturnsFullyQualifiedName()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("CustomerAggregate", "MyApp.Domain");
        AggregateModel model = new(typeSymbol, "api/customers", "customers");
        Assert.Equal("global::MyApp.Domain.CustomerAggregate", model.FullTypeName);
    }

    /// <summary>
    ///     Namespace should return containing namespace.
    /// </summary>
    [Fact]
    public void NamespaceReturnsContainingNamespace()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("CustomerAggregate", "MyApp.Domain.Aggregates");
        AggregateModel model = new(typeSymbol, "api/customers", "customers");
        Assert.Equal("MyApp.Domain.Aggregates", model.Namespace);
    }

    /// <summary>
    ///     RoutePrefix should return provided value.
    /// </summary>
    [Fact]
    public void RoutePrefixReturnsProvidedValue()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("CustomerAggregate", "MyApp.Domain");
        AggregateModel model = new(typeSymbol, "api/v1/customers", "customers");
        Assert.Equal("api/v1/customers", model.RoutePrefix);
    }

    /// <summary>
    ///     TypeName should return the short name.
    /// </summary>
    [Fact]
    public void TypeNameReturnsShortName()
    {
        INamedTypeSymbol typeSymbol = CreateNamedTypeSymbol("CustomerAggregate", "MyApp.Domain");
        AggregateModel model = new(typeSymbol, "api/customers", "customers");
        Assert.Equal("CustomerAggregate", model.TypeName);
    }
}