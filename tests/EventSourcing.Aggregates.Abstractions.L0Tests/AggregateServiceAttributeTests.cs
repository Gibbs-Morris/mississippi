using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateServiceAttribute" /> behavior.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates Abstractions")]
[AllureSubSuite("Aggregate Service Attribute")]
public sealed class AggregateServiceAttributeTests
{
    /// <summary>
    ///     Authorize property should default to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void AuthorizePropertyDefaultsToNull()
    {
        // Arrange & Act
        AggregateServiceAttribute attribute = new("users");

        // Assert
        Assert.Null(attribute.Authorize);
    }

    /// <summary>
    ///     Authorize property should be settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void AuthorizePropertyIsSettable()
    {
        // Arrange
        AggregateServiceAttribute attribute = new("users");

        // Act
        attribute.Authorize = "AdminPolicy";

        // Assert
        Assert.Equal("AdminPolicy", attribute.Authorize);
    }

    /// <summary>
    ///     Constructor should succeed with valid route.
    /// </summary>
    [Fact]
    [AllureFeature("Construction")]
    public void ConstructorSucceedsWithValidRoute()
    {
        // Arrange & Act
        AggregateServiceAttribute attribute = new("users");

        // Assert
        Assert.Equal("users", attribute.Route);
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when route is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenRouteIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AggregateServiceAttribute(null!));
    }

    /// <summary>
    ///     GenerateApi property should default to true.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void GenerateApiPropertyDefaultsToTrue()
    {
        // Arrange & Act
        AggregateServiceAttribute attribute = new("users");

        // Assert
        Assert.True(attribute.GenerateApi);
    }

    /// <summary>
    ///     GenerateApi property should be settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void GenerateApiPropertyIsSettable()
    {
        // Arrange
        AggregateServiceAttribute attribute = new("users");

        // Act
        attribute.GenerateApi = false;

        // Assert
        Assert.False(attribute.GenerateApi);
    }

    /// <summary>
    ///     Route property should return the value passed to constructor.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void RoutePropertyReturnsConstructorValue()
    {
        // Arrange & Act
        AggregateServiceAttribute attribute = new("orders");

        // Assert
        Assert.Equal("orders", attribute.Route);
    }
}