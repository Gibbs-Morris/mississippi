using System;
using System.Collections.Generic;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Abstractions;
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
    ///     AddInlet should register IInletStore pointing to InletStore.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersIInletStoreAsInletStore()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        IInletStore store = provider.GetRequiredService<IInletStore>();
        InletStore inletStore = provider.GetRequiredService<InletStore>();

        // Assert
        Assert.Same(inletStore, store);
    }

    /// <summary>
    ///     AddInlet should register IProjectionUpdateNotifier pointing to InletStore.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersIProjectionUpdateNotifierAsInletStore()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionUpdateNotifier notifier = provider.GetRequiredService<IProjectionUpdateNotifier>();
        InletStore inletStore = provider.GetRequiredService<InletStore>();

        // Assert
        Assert.Same(inletStore, notifier);
    }

    /// <summary>
    ///     AddInlet should register IStore pointing to InletStore.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersIStoreAsInletStore()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        InletStore inletStore = provider.GetRequiredService<InletStore>();

        // Assert
        Assert.Same(inletStore, store);
    }

    /// <summary>
    ///     AddInlet should register InletStore as singleton.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletRegistersInletStoreAsSingleton()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        using ServiceProvider provider = services.BuildServiceProvider();
        InletStore store1 = provider.GetRequiredService<InletStore>();
        InletStore store2 = provider.GetRequiredService<InletStore>();

        // Assert
        Assert.Same(store1, store2);
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