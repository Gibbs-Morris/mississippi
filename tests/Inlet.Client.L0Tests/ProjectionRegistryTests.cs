using System;

using Allure.Xunit.Attributes;


namespace Mississippi.Inlet.Client.L0Tests;

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
    ///     GetPath should throw InvalidOperationException for unregistered type.
    /// </summary>
    [Fact]
    [AllureFeature("Path Resolution")]
    public void GetPathThrowsForUnregisteredType()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sut.GetPath(typeof(TestProjection)));
    }

    /// <summary>
    ///     GetPath should throw ArgumentNullException for null type.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void GetPathWithNullTypeThrowsArgumentNullException()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.GetPath(null!));
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
        sut.Register<TestProjection>("cascade/test");

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
        sut.Register<TestProjection>("cascade/test");
        sut.Register<AnotherProjection>("cascade/another");

        // Assert
        Assert.Equal("cascade/test", sut.GetPath(typeof(TestProjection)));
        Assert.Equal("cascade/another", sut.GetPath(typeof(AnotherProjection)));
    }

    /// <summary>
    ///     Register should overwrite existing path for same type.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterOverwritesExistingPath()
    {
        // Arrange
        ProjectionRegistry sut = new();
        sut.Register<TestProjection>("cascade/old");

        // Act
        sut.Register<TestProjection>("cascade/new");
        string path = sut.GetPath(typeof(TestProjection));

        // Assert
        Assert.Equal("cascade/new", path);
    }

    /// <summary>
    ///     Register should store path for projection type.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterStoresPathForProjectionType()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act
        sut.Register<TestProjection>("cascade/test");
        string path = sut.GetPath(typeof(TestProjection));

        // Assert
        Assert.Equal("cascade/test", path);
    }

    /// <summary>
    ///     Register should throw ArgumentNullException for null path.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void RegisterWithNullPathThrowsArgumentNullException()
    {
        // Arrange
        ProjectionRegistry sut = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Register<TestProjection>(null!));
    }
}