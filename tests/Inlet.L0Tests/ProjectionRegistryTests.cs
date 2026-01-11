using System;

using Allure.Xunit.Attributes;


namespace Mississippi.Inlet.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionRegistry" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("Core")]
[AllureSubSuite("ProjectionRegistry")]
public sealed class ProjectionRegistryTests
{
    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    /// <param name="Value">The projection value.</param>
    private sealed record AnotherProjection(int Value);

    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    /// <param name="Name">The projection name.</param>
    private sealed record TestProjection(string Name);

    /// <summary>
    ///     GetRoute should throw InvalidOperationException for unregistered type.
    /// </summary>
    [Fact]
    [AllureFeature("Route Resolution")]
    public void GetRouteThrowsForUnregisteredType()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sut.GetRoute(typeof(TestProjection)));
    }

    /// <summary>
    ///     GetRoute should throw ArgumentNullException for null type.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void GetRouteWithNullTypeThrowsArgumentNullException()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.GetRoute(null!));
    }

    /// <summary>
    ///     IsRegistered should return false for unregistered type.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void IsRegisteredReturnsFalseForUnregisteredType()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act
        bool result = sut.IsRegistered(typeof(TestProjection));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     IsRegistered should return true for registered type.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void IsRegisteredReturnsTrueForRegisteredType()
    {
        // Arrange
        ProjectionRegistry sut = new();
        sut.Register<TestProjection>("/api/test");

        // Act
        bool result = sut.IsRegistered(typeof(TestProjection));

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     IsRegistered should throw ArgumentNullException for null type.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void IsRegisteredWithNullTypeThrowsArgumentNullException()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.IsRegistered(null!));
    }

    /// <summary>
    ///     Register should allow multiple projection types.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterAllowsMultipleProjectionTypes()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act
        sut.Register<TestProjection>("/api/test");
        sut.Register<AnotherProjection>("/api/another");

        // Assert
        Assert.Equal("/api/test", sut.GetRoute(typeof(TestProjection)));
        Assert.Equal("/api/another", sut.GetRoute(typeof(AnotherProjection)));
    }

    /// <summary>
    ///     Register should overwrite existing route for same type.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterOverwritesExistingRoute()
    {
        // Arrange
        ProjectionRegistry sut = new();
        sut.Register<TestProjection>("/api/old");

        // Act
        sut.Register<TestProjection>("/api/new");
        string route = sut.GetRoute(typeof(TestProjection));

        // Assert
        Assert.Equal("/api/new", route);
    }

    /// <summary>
    ///     Register should store route for projection type.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterStoresRouteForProjectionType()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act
        sut.Register<TestProjection>("/api/test");
        string route = sut.GetRoute(typeof(TestProjection));

        // Assert
        Assert.Equal("/api/test", route);
    }

    /// <summary>
    ///     Register should throw ArgumentNullException for null route.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void RegisterWithNullRouteThrowsArgumentNullException()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Register<TestProjection>(null!));
    }
}