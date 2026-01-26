using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Projection.Abstractions;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.ActionEffects;

/// <summary>
///     Tests for <see cref="ProjectionDtoRegistry" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Action Effects")]
[AllureSubSuite("ProjectionDtoRegistry")]
public sealed class ProjectionDtoRegistryTests
{
    /// <summary>
    ///     Another decorated DTO type for testing.
    /// </summary>
    [ProjectionPath("/test/another")]
    private static class AnotherDecoratedDto
    {
        public static Type AsType => typeof(AnotherDecoratedDto);
    }

    /// <summary>
    ///     Decorated DTO type for testing.
    /// </summary>
    [ProjectionPath("/test/decorated")]
    private static class DecoratedDto
    {
        public static Type AsType => typeof(DecoratedDto);
    }

    /// <summary>
    ///     Undecorated type for testing.
    /// </summary>
    private static class UndecoratedDto
    {
        public static Type AsType => typeof(UndecoratedDto);
    }

    /// <summary>
    ///     GetDtoType should return null for unregistered path.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetDtoTypeReturnsNullForUnregisteredPath()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        Type? result = registry.GetDtoType("/unregistered/path");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetDtoType should return the type for a registered path.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetDtoTypeReturnsTypeForRegisteredPath()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();
        registry.Register("/test/path", DecoratedDto.AsType);

        // Act
        Type? result = registry.GetDtoType("/test/path");

        // Assert
        Assert.Equal(DecoratedDto.AsType, result);
    }

    /// <summary>
    ///     GetDtoType should throw when path is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void GetDtoTypeThrowsWhenPathIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.GetDtoType(null!));
    }

    /// <summary>
    ///     GetPath should return null for unregistered type.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetPathReturnsNullForUnregisteredType()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        string? result = registry.GetPath(UndecoratedDto.AsType);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetPath should return the path for a registered type.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetPathReturnsPathForRegisteredType()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();
        registry.Register("/test/path", DecoratedDto.AsType);

        // Act
        string? result = registry.GetPath(DecoratedDto.AsType);

        // Assert
        Assert.Equal("/test/path", result);
    }

    /// <summary>
    ///     GetPath should throw when dtoType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void GetPathThrowsWhenDtoTypeIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.GetPath(null!));
    }

    /// <summary>
    ///     Register should add bidirectional mapping.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterAddsBidirectionalMapping()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        registry.Register("/test/path", DecoratedDto.AsType);

        // Assert
        Assert.Equal(DecoratedDto.AsType, registry.GetDtoType("/test/path"));
        Assert.Equal("/test/path", registry.GetPath(DecoratedDto.AsType));
    }

    /// <summary>
    ///     Register should throw when dtoType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void RegisterThrowsWhenDtoTypeIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.Register("/test/path", null!));
    }

    /// <summary>
    ///     Register should throw when path is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void RegisterThrowsWhenPathIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!, DecoratedDto.AsType));
    }

    /// <summary>
    ///     ScanAssemblies should register types with ProjectionPathAttribute.
    /// </summary>
    [Fact]
    [AllureFeature("Assembly Scanning")]
    public void ScanAssembliesRegistersDecoratedTypes()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        registry.ScanAssemblies(typeof(ProjectionDtoRegistryTests).Assembly);

        // Assert
        Assert.Equal(DecoratedDto.AsType, registry.GetDtoType("/test/decorated"));
        Assert.Equal(AnotherDecoratedDto.AsType, registry.GetDtoType("/test/another"));
    }

    /// <summary>
    ///     ScanAssemblies should throw when assemblies is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ScanAssembliesThrowsWhenAssembliesIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.ScanAssemblies(null!));
    }

    /// <summary>
    ///     TryGetDtoType should return false for unregistered path.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void TryGetDtoTypeReturnsFalseForUnregisteredPath()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        bool result = registry.TryGetDtoType("/unregistered/path", out Type? dtoType);

        // Assert
        Assert.False(result);
        Assert.Null(dtoType);
    }

    /// <summary>
    ///     TryGetDtoType should return true and the type for a registered path.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void TryGetDtoTypeReturnsTrueForRegisteredPath()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();
        registry.Register("/test/path", DecoratedDto.AsType);

        // Act
        bool result = registry.TryGetDtoType("/test/path", out Type? dtoType);

        // Assert
        Assert.True(result);
        Assert.Equal(DecoratedDto.AsType, dtoType);
    }

    /// <summary>
    ///     TryGetDtoType should throw when path is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void TryGetDtoTypeThrowsWhenPathIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.TryGetDtoType(null!, out Type? _));
    }

    /// <summary>
    ///     TryGetPath should return false for unregistered type.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void TryGetPathReturnsFalseForUnregisteredType()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        bool result = registry.TryGetPath(UndecoratedDto.AsType, out string? path);

        // Assert
        Assert.False(result);
        Assert.Null(path);
    }

    /// <summary>
    ///     TryGetPath should return true and the path for a registered type.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void TryGetPathReturnsTrueForRegisteredType()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();
        registry.Register("/test/path", DecoratedDto.AsType);

        // Act
        bool result = registry.TryGetPath(DecoratedDto.AsType, out string? path);

        // Assert
        Assert.True(result);
        Assert.Equal("/test/path", path);
    }

    /// <summary>
    ///     TryGetPath should throw when dtoType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void TryGetPathThrowsWhenDtoTypeIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.TryGetPath(null!, out string? _));
    }
}