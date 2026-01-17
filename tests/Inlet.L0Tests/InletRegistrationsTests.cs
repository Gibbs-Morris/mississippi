using System;
using System.Collections.Generic;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Abstractions;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.L0Tests;

/// <summary>
///     Tests for <see cref="InletRegistrations" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("Configuration")]
[AllureSubSuite("InletRegistrations")]
public sealed class InletRegistrationsTests
{
    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    /// <param name="Name">The projection name.</param>
    private sealed record TestProjection(string Name);

    /// <summary>
    ///     AddInlet should register IInletStore as CompositeInletStore.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersIInletStoreAsCompositeInletStore()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        IInletStore store = provider.GetRequiredService<IInletStore>();

        // Assert
        Assert.IsType<CompositeInletStore>(store);
    }

    /// <summary>
    ///     AddInlet should register IProjectionCache as ProjectionCache singleton.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersIProjectionCacheAsSingleton()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionCache cache1 = provider.GetRequiredService<IProjectionCache>();
        IProjectionCache cache2 = provider.GetRequiredService<IProjectionCache>();

        // Assert
        Assert.IsType<ProjectionCache>(cache1);
        Assert.Same(cache1, cache2);
    }

    /// <summary>
    ///     AddInlet should register IProjectionUpdateNotifier as ProjectionNotifier.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersIProjectionUpdateNotifierAsProjectionNotifier()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionUpdateNotifier notifier = provider.GetRequiredService<IProjectionUpdateNotifier>();

        // Assert
        Assert.IsType<ProjectionNotifier>(notifier);
    }

    /// <summary>
    ///     AddInlet should register IStore as singleton.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersIStoreAsSingleton()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store1 = provider.GetRequiredService<IStore>();
        IStore store2 = provider.GetRequiredService<IStore>();

        // Assert
        Assert.Same(store1, store2);
    }

    /// <summary>
    ///     AddInlet should register IStore as Store.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersIStoreAsStore()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();

        // Assert
        Assert.IsType<Store>(store);
    }

    /// <summary>
    ///     AddInlet should register IProjectionRegistry.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersProjectionRegistry()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionRegistry registry = provider.GetRequiredService<IProjectionRegistry>();

        // Assert
        Assert.NotNull(registry);
        Assert.IsType<ProjectionRegistry>(registry);
    }

    /// <summary>
    ///     AddInlet should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void AddInletWithNullServicesThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddInlet());
    }

    /// <summary>
    ///     AddProjectionPath should register path in IConfigureProjectionRegistry.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddProjectionPathRegistersConfiguration()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        services.AddProjectionPath<TestProjection>("cascade/test");
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IConfigureProjectionRegistry> configs = provider.GetServices<IConfigureProjectionRegistry>();

        // Assert
        Assert.Single(configs);
    }

    /// <summary>
    ///     AddProjectionPath should throw ArgumentNullException when path is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void AddProjectionPathWithNullPathThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddProjectionPath<TestProjection>(null!));
    }

    /// <summary>
    ///     AddProjectionPath should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void AddProjectionPathWithNullServicesThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddProjectionPath<TestProjection>("cascade/test"));
    }

    /// <summary>
    ///     AddProjectionPath configuration should register path in registry.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void ProjectionPathConfigurationRegistersPathInRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddInlet();
        services.AddProjectionPath<TestProjection>("cascade/test");
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act - Get the config and registry, then call Configure
        IEnumerable<IConfigureProjectionRegistry> configs = provider.GetServices<IConfigureProjectionRegistry>();
        IProjectionRegistry registry = provider.GetRequiredService<IProjectionRegistry>();
        foreach (IConfigureProjectionRegistry config in configs)
        {
            config.Configure(registry);
        }

        // Assert - The path should now be registered
        string path = registry.GetPath(typeof(TestProjection));
        Assert.Equal("cascade/test", path);
    }
}