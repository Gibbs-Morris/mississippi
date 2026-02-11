using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Inlet.Silo.Abstractions;


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
        TestMississippiSiloBuilder builder = new(services);

        // Act
        builder.AddInletSilo();
        IMississippiSiloBuilder result = builder.AddInletSilo();

        // Assert
        Assert.Same(builder, result);
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
        TestMississippiSiloBuilder builder = new(services);
        builder.AddInletSilo();

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
        TestMississippiSiloBuilder builder = new(services);
        builder.AddInletSilo();

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
        TestMississippiSiloBuilder builder = new(services);

        // Act
        IMississippiSiloBuilder result = builder.AddInletSilo();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddInletSilo should throw when services is null.
    /// </summary>
    [Fact]
    public void AddInletSiloThrowsWhenServicesNull()
    {
        // Arrange
        IMississippiSiloBuilder? builder = null;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => builder!.AddInletSilo());
        Assert.Equal("builder", exception.ParamName);
    }

    /// <summary>
    ///     RegisterProjectionBrookMappings populates registry with configured mappings.
    /// </summary>
    [Fact]
    public void RegisterProjectionBrookMappingsPopulatesRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);

        // Act
        builder.RegisterProjectionBrookMappings(registry =>
        {
            registry.Register("bank-account-balance", "SPRING.BANKING.ACCOUNT");
            registry.Register("money-transfer-status", "SPRING.BANKING.TRANSFER");
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        string[] paths = registry.GetAllPaths().ToArray();
        Assert.Equal(2, paths.Length);
        Assert.Contains("bank-account-balance", paths);
        Assert.Contains("money-transfer-status", paths);
        Assert.Equal("SPRING.BANKING.ACCOUNT", registry.GetBrookName("bank-account-balance"));
        Assert.Equal("SPRING.BANKING.TRANSFER", registry.GetBrookName("money-transfer-status"));
    }

    /// <summary>
    ///     RegisterProjectionBrookMappings replaces existing registry from AddInletSilo.
    /// </summary>
    [Fact]
    public void RegisterProjectionBrookMappingsReplacesExistingRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);
        builder.AddInletSilo();

        // Act
        builder.RegisterProjectionBrookMappings(registry =>
        {
            registry.Register("test-projection", "TEST.MODULE.PROJECTION");
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert - Should have the configured mappings, not the empty default
        string[] paths = registry.GetAllPaths().ToArray();
        Assert.Single(paths);
        Assert.Equal("test-projection", paths[0]);
        Assert.Equal("TEST.MODULE.PROJECTION", registry.GetBrookName("test-projection"));
    }

    /// <summary>
    ///     RegisterProjectionBrookMappings returns the same builder for chaining.
    /// </summary>
    [Fact]
    public void RegisterProjectionBrookMappingsReturnsSameBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);

        // Act
        IMississippiSiloBuilder result = builder.RegisterProjectionBrookMappings(_ => { });

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     RegisterProjectionBrookMappings throws when builder is null.
    /// </summary>
    [Fact]
    public void RegisterProjectionBrookMappingsThrowsWhenBuilderNull()
    {
        // Arrange
        IMississippiSiloBuilder? builder = null;

        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => builder!.RegisterProjectionBrookMappings(_ => { }));
        Assert.Equal("builder", exception.ParamName);
    }

    /// <summary>
    ///     RegisterProjectionBrookMappings throws when configure is null.
    /// </summary>
    [Fact]
    public void RegisterProjectionBrookMappingsThrowsWhenConfigureNull()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);

        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => builder.RegisterProjectionBrookMappings(null!));
        Assert.Equal("configure", exception.ParamName);
    }

    /// <summary>
    ///     RegisterProjectionBrookMappings with empty configure creates empty registry.
    /// </summary>
    [Fact]
    public void RegisterProjectionBrookMappingsWithEmptyConfigureCreatesEmptyRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);
        builder.RegisterProjectionBrookMappings(_ => { });

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        Assert.NotNull(registry);
        Assert.Empty(registry.GetAllPaths());
    }
}