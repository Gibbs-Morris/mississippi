using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;


namespace Mississippi.Ripples.Client.L0Tests;

/// <summary>
/// Tests for <see cref="ConfigurableProjectionRouteProvider"/>.
/// </summary>
[AllureParentSuite("Mississippi.Ripples")]
[AllureSuite("Client")]
[AllureSubSuite("ConfigurableProjectionRouteProvider")]
public sealed class ConfigurableProjectionRouteProviderTests
{
    /// <summary>
    /// Test projection record for unit tests.
    /// </summary>
    [SuppressMessage("Design", "S2094:Classes should not be empty", Justification = "Test stub")]
    private sealed class TestProjection;

    /// <summary>
    /// Another test projection record for unit tests.
    /// </summary>
    [SuppressMessage("Design", "S2094:Classes should not be empty", Justification = "Test stub")]
    private sealed class AnotherProjection;

    /// <summary>
    /// GetRoute generic should return the registered route.
    /// </summary>
    [Fact]
    [AllureFeature("Route Retrieval")]
    public void GetRouteGenericReturnsRegisteredRoute()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();
        sut.Register<TestProjection>("/api/projections/test");

        // Act
        string result = sut.GetRoute<TestProjection>();

        // Assert
        result.Should().Be("/api/projections/test");
    }

    /// <summary>
    /// GetRoute non-generic should return the registered route.
    /// </summary>
    [Fact]
    [AllureFeature("Route Retrieval")]
    [SuppressMessage("Performance", "CA2263:Prefer generic overload when type is known", Justification = "Testing non-generic overload")]
    public void GetRouteNonGenericReturnsRegisteredRoute()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();
        sut.Register<TestProjection>("/api/projections/test");

        // Act
        string result = sut.GetRoute(typeof(TestProjection));

        // Assert
        result.Should().Be("/api/projections/test");
    }

    /// <summary>
    /// GetRoute generic should throw when route is not registered.
    /// </summary>
    [Fact]
    [AllureFeature("Route Retrieval")]
    public void GetRouteGenericThrowsWhenRouteNotRegistered()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();

        // Act
        Action act = () => sut.GetRoute<TestProjection>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestProjection*");
    }

    /// <summary>
    /// GetRoute non-generic should throw when route is not registered.
    /// </summary>
    [Fact]
    [AllureFeature("Route Retrieval")]
    [SuppressMessage("Performance", "CA2263:Prefer generic overload when type is known", Justification = "Testing non-generic overload")]
    public void GetRouteNonGenericThrowsWhenRouteNotRegistered()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();

        // Act
        Action act = () => sut.GetRoute(typeof(TestProjection));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestProjection*");
    }

    /// <summary>
    /// TryGetRoute generic should return true and the route when registered.
    /// </summary>
    [Fact]
    [AllureFeature("Route Retrieval")]
    public void TryGetRouteGenericReturnsTrueAndRouteWhenRegistered()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();
        sut.Register<TestProjection>("/api/projections/test");

        // Act
        bool result = sut.TryGetRoute<TestProjection>(out string? route);

        // Assert
        result.Should().BeTrue();
        route.Should().Be("/api/projections/test");
    }

    /// <summary>
    /// TryGetRoute generic should return false when route is not registered.
    /// </summary>
    [Fact]
    [AllureFeature("Route Retrieval")]
    public void TryGetRouteGenericReturnsFalseWhenRouteNotRegistered()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();

        // Act
        bool result = sut.TryGetRoute<TestProjection>(out string? route);

        // Assert
        result.Should().BeFalse();
        route.Should().BeNull();
    }

    /// <summary>
    /// TryGetRoute non-generic should return true and the route when registered.
    /// </summary>
    [Fact]
    [AllureFeature("Route Retrieval")]
    [SuppressMessage("Performance", "CA2263:Prefer generic overload when type is known", Justification = "Testing non-generic overload")]
    public void TryGetRouteNonGenericReturnsTrueAndRouteWhenRegistered()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();
        sut.Register<TestProjection>("/api/projections/test");

        // Act
        bool result = sut.TryGetRoute(typeof(TestProjection), out string? route);

        // Assert
        result.Should().BeTrue();
        route.Should().Be("/api/projections/test");
    }

    /// <summary>
    /// TryGetRoute non-generic should return false when route is not registered.
    /// </summary>
    [Fact]
    [AllureFeature("Route Retrieval")]
    [SuppressMessage("Performance", "CA2263:Prefer generic overload when type is known", Justification = "Testing non-generic overload")]
    public void TryGetRouteNonGenericReturnsFalseWhenRouteNotRegistered()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();

        // Act
        bool result = sut.TryGetRoute(typeof(TestProjection), out string? route);

        // Assert
        result.Should().BeFalse();
        route.Should().BeNull();
    }

    /// <summary>
    /// Register should support chaining for multiple types.
    /// </summary>
    [Fact]
    [AllureFeature("Route Registration")]
    public void RegisterSupportsChaining()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();

        // Act
        sut.Register<TestProjection>("/api/test")
           .Register<AnotherProjection>("/api/another");

        // Assert
        sut.GetRoute<TestProjection>().Should().Be("/api/test");
        sut.GetRoute<AnotherProjection>().Should().Be("/api/another");
    }

    /// <summary>
    /// Register should overwrite previous route when called twice with same type.
    /// </summary>
    [Fact]
    [AllureFeature("Route Registration")]
    public void RegisterOverwritesPreviousRoute()
    {
        // Arrange
        ConfigurableProjectionRouteProvider sut = new();
        sut.Register<TestProjection>("/api/old");

        // Act
        sut.Register<TestProjection>("/api/new");

        // Assert
        sut.GetRoute<TestProjection>().Should().Be("/api/new");
    }
}
