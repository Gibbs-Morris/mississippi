using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Tests for <see cref="ProjectionDtoRegistry" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("ActionEffects")]
[AllureSubSuite("ProjectionDtoRegistry")]
public sealed class ProjectionDtoRegistryTests
{
    /// <summary>
    ///     GetDtoType returns null for unknown path.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetDtoTypeReturnsNullForUnknownPath()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        Type? result = registry.GetDtoType("unknown/path");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetDtoType returns registered type.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetDtoTypeReturnsRegisteredType()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();
        registry.Register("test/path", typeof(TestProjection));

        // Act
        Type? result = registry.GetDtoType("test/path");

        // Assert
        Assert.Equal(typeof(TestProjection), result);
    }

    /// <summary>
    ///     GetDtoType throws ArgumentNullException when path is null.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetDtoTypeThrowsWhenPathIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.GetDtoType(null!));
    }

    /// <summary>
    ///     GetPath returns null for unknown type.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetPathReturnsNullForUnknownType()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        string? result = registry.GetPath(typeof(TestProjection));

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetPath returns registered path.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetPathReturnsRegisteredPath()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();
        registry.Register("test/path", typeof(TestProjection));

        // Act
        string? result = registry.GetPath(typeof(TestProjection));

        // Assert
        Assert.Equal("test/path", result);
    }

    /// <summary>
    ///     GetPath throws ArgumentNullException when dtoType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetPathThrowsWhenDtoTypeIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.GetPath(null!));
    }

    /// <summary>
    ///     Register adds bidirectional mapping.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterAddsBidirectionalMapping()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        registry.Register("bidirectional/test", typeof(TestProjection));

        // Assert
        Assert.Equal(typeof(TestProjection), registry.GetDtoType("bidirectional/test"));
        Assert.Equal("bidirectional/test", registry.GetPath(typeof(TestProjection)));
    }

    /// <summary>
    ///     Register overwrites existing mapping for same path.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterOverwritesExistingMappingForSamePath()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();
        registry.Register("overwrite/path", typeof(TestProjection));

        // Act
        registry.Register("overwrite/path", typeof(AlternateTestProjection));

        // Assert
        Assert.Equal(typeof(AlternateTestProjection), registry.GetDtoType("overwrite/path"));
    }

    /// <summary>
    ///     Register throws ArgumentNullException when dtoType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterThrowsWhenDtoTypeIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.Register("test/path", null!));
    }

    /// <summary>
    ///     Register throws ArgumentNullException when path is null.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterThrowsWhenPathIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!, typeof(TestProjection)));
    }

    /// <summary>
    ///     ScanAssemblies handles empty array.
    /// </summary>
    [Fact]
    [AllureFeature("Scanning")]
    public void ScanAssembliesHandlesEmptyArray()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act (should not throw)
        registry.ScanAssemblies();

        // Assert - registry is empty
        Assert.Null(registry.GetDtoType("any/path"));
    }

    /// <summary>
    ///     ScanAssemblies registers types with ProjectionPathAttribute.
    /// </summary>
    [Fact]
    [AllureFeature("Scanning")]
    public void ScanAssembliesRegistersDecoratedTypes()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        registry.ScanAssemblies(typeof(DecoratedTestProjection).Assembly);

        // Assert
        Assert.Equal(typeof(DecoratedTestProjection), registry.GetDtoType("test/decorated"));
    }

    /// <summary>
    ///     ScanAssemblies scans multiple assemblies.
    /// </summary>
    [Fact]
    [AllureFeature("Scanning")]
    public void ScanAssembliesScansMultipleAssemblies()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act - scan the test assembly twice (should not throw)
        registry.ScanAssemblies(typeof(DecoratedTestProjection).Assembly, typeof(DecoratedTestProjection).Assembly);

        // Assert
        Assert.Equal(typeof(DecoratedTestProjection), registry.GetDtoType("test/decorated"));
    }

    /// <summary>
    ///     ScanAssemblies throws ArgumentNullException when assemblies is null.
    /// </summary>
    [Fact]
    [AllureFeature("Scanning")]
    public void ScanAssembliesThrowsWhenAssembliesIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.ScanAssemblies(null!));
    }

    /// <summary>
    ///     TryGetDtoType returns false when not found.
    /// </summary>
    [Fact]
    [AllureFeature("TryGet")]
    public void TryGetDtoTypeReturnsFalseWhenNotFound()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        bool found = registry.TryGetDtoType("missing/path", out Type? dtoType);

        // Assert
        Assert.False(found);
        Assert.Null(dtoType);
    }

    /// <summary>
    ///     TryGetDtoType returns true and outputs type when found.
    /// </summary>
    [Fact]
    [AllureFeature("TryGet")]
    public void TryGetDtoTypeReturnsTrueWhenFound()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();
        registry.Register("try/path", typeof(TestProjection));

        // Act
        bool found = registry.TryGetDtoType("try/path", out Type? dtoType);

        // Assert
        Assert.True(found);
        Assert.Equal(typeof(TestProjection), dtoType);
    }

    /// <summary>
    ///     TryGetDtoType throws ArgumentNullException when path is null.
    /// </summary>
    [Fact]
    [AllureFeature("TryGet")]
    public void TryGetDtoTypeThrowsWhenPathIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.TryGetDtoType(null!, out var _));
    }

    /// <summary>
    ///     TryGetPath returns false when not found.
    /// </summary>
    [Fact]
    [AllureFeature("TryGet")]
    public void TryGetPathReturnsFalseWhenNotFound()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act
        bool found = registry.TryGetPath(typeof(TestProjection), out string? path);

        // Assert
        Assert.False(found);
        Assert.Null(path);
    }

    /// <summary>
    ///     TryGetPath returns true and outputs path when found.
    /// </summary>
    [Fact]
    [AllureFeature("TryGet")]
    public void TryGetPathReturnsTrueWhenFound()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();
        registry.Register("try/get/path", typeof(TestProjection));

        // Act
        bool found = registry.TryGetPath(typeof(TestProjection), out string? path);

        // Assert
        Assert.True(found);
        Assert.Equal("try/get/path", path);
    }

    /// <summary>
    ///     TryGetPath throws ArgumentNullException when dtoType is null.
    /// </summary>
    [Fact]
    [AllureFeature("TryGet")]
    public void TryGetPathThrowsWhenDtoTypeIsNull()
    {
        // Arrange
        ProjectionDtoRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.TryGetPath(null!, out var _));
    }
}