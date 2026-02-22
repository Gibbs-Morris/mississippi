using System;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Silo.Abstractions;
using Mississippi.Inlet.Silo.L0Tests.Infrastructure;


namespace Mississippi.Inlet.Silo.L0Tests;

/// <summary>
///     Tests for <see cref="InletSiloRegistrations" />.
/// </summary>
public sealed class InletSiloRegistrationsTests
{
    /// <summary>
    ///     AddInletSilo can be called multiple times.
    /// </summary>
    [Fact]
    public void AddInletSiloCanBeCalledMultipleTimes()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInletSilo();
        IServiceCollection result = services.AddInletSilo();

        // Assert
        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IProjectionBrookRegistry>());
    }

    /// <summary>
    ///     AddInletSilo should register as singleton.
    /// </summary>
    [Fact]
    public void AddInletSiloRegistersAsSingleton()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddInletSilo();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry1 = provider.GetRequiredService<IProjectionBrookRegistry>();
        IProjectionBrookRegistry registry2 = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        Assert.Same(registry1, registry2);
    }

    /// <summary>
    ///     AddInletSilo should register IProjectionBrookRegistry.
    /// </summary>
    [Fact]
    public void AddInletSiloRegistersIProjectionBrookRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddInletSilo();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        Assert.NotNull(registry);
    }

    /// <summary>
    ///     AddInletSilo should return the same services collection for chaining.
    /// </summary>
    [Fact]
    public void AddInletSiloReturnsSameCollection()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        IServiceCollection result = services.AddInletSilo();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletSilo should throw when services is null.
    /// </summary>
    [Fact]
    public void AddInletSiloThrowsWhenServicesNull()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => services.AddInletSilo());
        Assert.Equal("services", exception.ParamName);
    }

    /// <summary>
    ///     ScanProjectionAssemblies discovers types with ProjectionPathAttribute.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesDiscoversProjectionPathAttribute()
    {
        // Arrange
        ServiceCollection services = [];
        Assembly testAssembly = typeof(PathOnlyProjection).Assembly;

        // Act
        services.ScanProjectionAssemblies(testAssembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        string[] paths = registry.GetAllPaths().ToArray();
        Assert.Contains("/api/path-only-projection", paths);
    }

    /// <summary>
    ///     ScanProjectionAssemblies replaces existing registry.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesReplacesExistingRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddInletSilo();
        Assembly testAssembly = typeof(PathOnlyProjection).Assembly;

        // Act
        services.ScanProjectionAssemblies(testAssembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert - Should have the scanned projections, not the empty default
        string[] paths = registry.GetAllPaths().ToArray();
        Assert.Contains("/api/path-only-projection", paths);
    }

    /// <summary>
    ///     ScanProjectionAssemblies should return the same services collection for chaining.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesReturnsSameCollection()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        IServiceCollection result = services.ScanProjectionAssemblies();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     ScanProjectionAssemblies should throw when assemblies is null.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesThrowsWhenAssembliesNull()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => services.ScanProjectionAssemblies(null!));
        Assert.Equal("assemblies", exception.ParamName);
    }

    /// <summary>
    ///     ScanProjectionAssemblies should throw when services is null.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesThrowsWhenServicesNull()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => services.ScanProjectionAssemblies());
        Assert.Equal("services", exception.ParamName);
    }

    /// <summary>
    ///     ScanProjectionAssemblies uses BrookNameAttribute value when present.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesUsesBrookNameAttributeWhenPresent()
    {
        // Arrange
        ServiceCollection services = [];
        Assembly testAssembly = typeof(BrookNamedProjection).Assembly;

        // Act
        services.ScanProjectionAssemblies(testAssembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        string? brookName = registry.GetBrookName("/api/brook-named-projection");
        Assert.Equal("TEST.MODULE.BROOKNAME", brookName);
    }

    /// <summary>
    ///     ScanProjectionAssemblies uses path as brook name when BrookNameAttribute is absent.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesUsesPathAsBrookNameByDefault()
    {
        // Arrange
        ServiceCollection services = [];
        Assembly testAssembly = typeof(PathOnlyProjection).Assembly;

        // Act
        services.ScanProjectionAssemblies(testAssembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        string? brookName = registry.GetBrookName("/api/path-only-projection");
        Assert.Equal("/api/path-only-projection", brookName);
    }

    /// <summary>
    ///     ScanProjectionAssemblies with no assemblies creates empty registry.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesWithNoAssembliesCreatesEmptyRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        services.ScanProjectionAssemblies();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        Assert.NotNull(registry);
        Assert.Empty(registry.GetAllPaths());
    }
}