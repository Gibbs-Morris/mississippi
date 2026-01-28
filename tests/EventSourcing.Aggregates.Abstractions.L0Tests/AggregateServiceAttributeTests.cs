using System;


namespace Mississippi.EventSourcing.Aggregates.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateServiceAttribute" /> behavior.
/// </summary>
public sealed class AggregateServiceAttributeTests
{
    /// <summary>
    ///     Authorize property should default to null.
    /// </summary>
    [Fact]
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
    public void ConstructorThrowsWhenRouteIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AggregateServiceAttribute(null!));
    }

    /// <summary>
    ///     GenerateApi property should default to true.
    /// </summary>
    [Fact]
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
    public void RoutePropertyReturnsConstructorValue()
    {
        // Arrange & Act
        AggregateServiceAttribute attribute = new("orders");

        // Assert
        Assert.Equal("orders", attribute.Route);
    }
}