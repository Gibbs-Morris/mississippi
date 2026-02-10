using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Silo.Abstractions;


namespace Mississippi.Inlet.Silo.L0Tests;

/// <summary>
///     Tests for ProjectionBrookRegistry via <see cref="IProjectionBrookRegistry" />.
/// </summary>
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
        TestMississippiSiloBuilder builder = new(services);
        builder.AddInletSilo();
        serviceProvider = services.BuildServiceProvider();
        registry = serviceProvider.GetRequiredService<IProjectionBrookRegistry>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        serviceProvider.Dispose();
    }

    /// <summary>
    ///     GetAllPaths should return empty for new registry.
    /// </summary>
    [Fact]
    public void GetAllPathsReturnsEmptyForNewRegistry()
    {
        // Act
        string[] paths = registry.GetAllPaths().ToArray();

        // Assert
        Assert.Empty(paths);
    }

    /// <summary>
    ///     GetAllPaths should return registered projections.
    /// </summary>
    [Fact]
    public void GetAllPathsReturnsRegisteredProjections()
    {
        // Arrange
        registry.Register("cascade/channels", "brook1");
        registry.Register("cascade/users", "brook2");

        // Act
        string[] paths = registry.GetAllPaths().ToArray();

        // Assert
        Assert.Equal(2, paths.Length);
        Assert.Contains("cascade/channels", paths);
        Assert.Contains("cascade/users", paths);
    }

    /// <summary>
    ///     GetBrookName should return null for unknown projection.
    /// </summary>
    [Fact]
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
    ///     GetBrookName should throw when path is null.
    /// </summary>
    [Fact]
    public void GetBrookNameThrowsWhenPathNull()
    {
        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => registry.GetBrookName(null!));
        Assert.Equal("path", exception.ParamName);
    }

    /// <summary>
    ///     Register should overwrite existing mapping.
    /// </summary>
    [Fact]
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
    public void RegisterThrowsWhenBrookNameNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => registry.Register("Projection", null!));
        Assert.Equal("brookName", exception.ParamName);
    }

    /// <summary>
    ///     Register should throw when path is null.
    /// </summary>
    [Fact]
    public void RegisterThrowsWhenPathNull()
    {
        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => registry.Register(null!, "brook"));
        Assert.Equal("path", exception.ParamName);
    }

    /// <summary>
    ///     TryGetBrookName should return false for unknown projection.
    /// </summary>
    [Fact]
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
    ///     TryGetBrookName should throw when path is null.
    /// </summary>
    [Fact]
    public void TryGetBrookNameThrowsWhenPathNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => registry.TryGetBrookName(null!, out string? _));
        Assert.Equal("path", exception.ParamName);
    }
}