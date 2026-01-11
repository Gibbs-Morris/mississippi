using System;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet.Orleans.L0Tests;

/// <summary>
///     Tests for ProjectionBrookRegistry via <see cref="IProjectionBrookRegistry" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Orleans")]
[AllureSuite("Registry")]
[AllureSubSuite("ProjectionBrookRegistry")]
public sealed class ProjectionBrookRegistryTests : IDisposable
{
    private readonly IProjectionBrookRegistry registry;

    private readonly ServiceProvider serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionBrookRegistryTests" /> class.
    /// </summary>
    public ProjectionBrookRegistryTests()
    {
        ServiceCollection services = [];
        services.AddInletOrleans();
        serviceProvider = services.BuildServiceProvider();
        registry = serviceProvider.GetRequiredService<IProjectionBrookRegistry>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        serviceProvider.Dispose();
    }

    /// <summary>
    ///     GetAllProjectionTypes should return empty for new registry.
    /// </summary>
    [Fact]
    [AllureFeature("Enumeration")]
    public void GetAllProjectionTypesReturnsEmptyForNewRegistry()
    {
        // Act
        string[] types = registry.GetAllProjectionTypes().ToArray();

        // Assert
        Assert.Empty(types);
    }

    /// <summary>
    ///     GetAllProjectionTypes should return registered projections.
    /// </summary>
    [Fact]
    [AllureFeature("Enumeration")]
    public void GetAllProjectionTypesReturnsRegisteredProjections()
    {
        // Arrange
        registry.Register("Projection1", "brook1");
        registry.Register("Projection2", "brook2");

        // Act
        string[] types = registry.GetAllProjectionTypes().ToArray();

        // Assert
        Assert.Equal(2, types.Length);
        Assert.Contains("Projection1", types);
        Assert.Contains("Projection2", types);
    }

    /// <summary>
    ///     GetBrookName should return null for unknown projection.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetBrookNameReturnsNullForUnknownProjection()
    {
        // Act
        string? brookName = registry.GetBrookName("UnknownProjection");

        // Assert
        Assert.Null(brookName);
    }

    /// <summary>
    ///     GetBrookName should return registered brook name.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void GetBrookNameReturnsRegisteredBrookName()
    {
        // Arrange
        registry.Register("LookupProjection", "lookup-brook");

        // Act
        string? brookName = registry.GetBrookName("LookupProjection");

        // Assert
        Assert.Equal("lookup-brook", brookName);
    }

    /// <summary>
    ///     GetBrookName should throw when projectionTypeName is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void GetBrookNameThrowsWhenProjectionTypeNameNull()
    {
        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => registry.GetBrookName(null!));
        Assert.Equal("projectionTypeName", exception.ParamName);
    }

    /// <summary>
    ///     Register should overwrite existing mapping.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterOverwritesExistingMapping()
    {
        // Arrange
        registry.Register("TestProjection", "old-brook");

        // Act
        registry.Register("TestProjection", "new-brook");

        // Assert
        string? brookName = registry.GetBrookName("TestProjection");
        Assert.Equal("new-brook", brookName);
    }

    /// <summary>
    ///     Register should store the mapping.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void RegisterStoresMapping()
    {
        // Act
        registry.Register("TestProjection", "test-brook");

        // Assert
        string? brookName = registry.GetBrookName("TestProjection");
        Assert.Equal("test-brook", brookName);
    }

    /// <summary>
    ///     Register should throw when brookName is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void RegisterThrowsWhenBrookNameNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => registry.Register("Projection", null!));
        Assert.Equal("brookName", exception.ParamName);
    }

    /// <summary>
    ///     Register should throw when projectionTypeName is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void RegisterThrowsWhenProjectionTypeNameNull()
    {
        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => registry.Register(null!, "brook"));
        Assert.Equal("projectionTypeName", exception.ParamName);
    }

    /// <summary>
    ///     TryGetBrookName should return false for unknown projection.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void TryGetBrookNameReturnsFalseForUnknownProjection()
    {
        // Act
        bool result = registry.TryGetBrookName("UnknownProjection", out string? brookName);

        // Assert
        Assert.False(result);
        Assert.Null(brookName);
    }

    /// <summary>
    ///     TryGetBrookName should return true and brook name for registered projection.
    /// </summary>
    [Fact]
    [AllureFeature("Lookup")]
    public void TryGetBrookNameReturnsTrueForRegisteredProjection()
    {
        // Arrange
        registry.Register("TestProjection", "test-brook");

        // Act
        bool result = registry.TryGetBrookName("TestProjection", out string? brookName);

        // Assert
        Assert.True(result);
        Assert.Equal("test-brook", brookName);
    }

    /// <summary>
    ///     TryGetBrookName should throw when projectionTypeName is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void TryGetBrookNameThrowsWhenProjectionTypeNameNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => registry.TryGetBrookName(null!, out string? _));
        Assert.Equal("projectionTypeName", exception.ParamName);
    }
}