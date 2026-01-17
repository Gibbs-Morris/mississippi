using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Projection.Abstractions;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionPathAttribute" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Projection.Abstractions")]
[AllureSuite("Attributes")]
[AllureSubSuite("ProjectionPathAttribute")]
public sealed class ProjectionPathAttributeTests
{
    /// <summary>
    ///     Constructor should store the path.
    /// </summary>
    [Fact]
    [AllureFeature("Construction")]
    public void ConstructorStoresPath()
    {
        // Arrange & Act
        ProjectionPathAttribute attribute = new("cascade/channels");

        // Assert
        Assert.Equal("cascade/channels", attribute.Path);
    }

    /// <summary>
    ///     Constructor should throw when path is empty.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenPathIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new ProjectionPathAttribute(string.Empty));
    }

    /// <summary>
    ///     Constructor should throw when path is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenPathIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectionPathAttribute(null!));
    }

    /// <summary>
    ///     Constructor should throw when path is whitespace.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenPathIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new ProjectionPathAttribute("   "));
    }

    /// <summary>
    ///     GetEntityPath should return path with entity ID.
    /// </summary>
    [Fact]
    [AllureFeature("Path Construction")]
    public void GetEntityPathReturnsPathWithEntityId()
    {
        // Arrange
        ProjectionPathAttribute attribute = new("cascade/channels");

        // Act
        string result = attribute.GetEntityPath("abc-123");

        // Assert
        Assert.Equal("cascade/channels/abc-123", result);
    }

    /// <summary>
    ///     GetEntityPath should throw when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void GetEntityPathThrowsWhenEntityIdIsNull()
    {
        // Arrange
        ProjectionPathAttribute attribute = new("cascade/channels");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => attribute.GetEntityPath(null!));
    }

    /// <summary>
    ///     GetVersionedEntityPath should handle negative version.
    /// </summary>
    [Fact]
    [AllureFeature("Path Construction")]
    public void GetVersionedEntityPathHandlesNegativeVersion()
    {
        // Arrange
        ProjectionPathAttribute attribute = new("cascade/channels");

        // Act
        string result = attribute.GetVersionedEntityPath("abc-123", -1);

        // Assert
        Assert.Equal("cascade/channels/abc-123/-1", result);
    }

    /// <summary>
    ///     GetVersionedEntityPath should handle zero version.
    /// </summary>
    [Fact]
    [AllureFeature("Path Construction")]
    public void GetVersionedEntityPathHandlesZeroVersion()
    {
        // Arrange
        ProjectionPathAttribute attribute = new("cascade/channels");

        // Act
        string result = attribute.GetVersionedEntityPath("abc-123", 0);

        // Assert
        Assert.Equal("cascade/channels/abc-123/0", result);
    }

    /// <summary>
    ///     GetVersionedEntityPath should return path with entity ID and version.
    /// </summary>
    [Fact]
    [AllureFeature("Path Construction")]
    public void GetVersionedEntityPathReturnsPathWithEntityIdAndVersion()
    {
        // Arrange
        ProjectionPathAttribute attribute = new("cascade/channels");

        // Act
        string result = attribute.GetVersionedEntityPath("abc-123", 42);

        // Assert
        Assert.Equal("cascade/channels/abc-123/42", result);
    }

    /// <summary>
    ///     GetVersionedEntityPath should throw when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void GetVersionedEntityPathThrowsWhenEntityIdIsNull()
    {
        // Arrange
        ProjectionPathAttribute attribute = new("cascade/channels");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => attribute.GetVersionedEntityPath(null!, 42));
    }
}