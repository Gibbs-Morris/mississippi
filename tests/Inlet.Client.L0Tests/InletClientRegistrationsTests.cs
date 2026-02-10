using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for <see cref="InletClientRegistrations" />.
/// </summary>
public sealed class InletClientRegistrationsTests
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
    public void AddInletRegistersIInletStoreAsCompositeInletStore()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiClientBuilder builder = new(services);

        // Act
        builder.AddInletClient();
        using ServiceProvider provider = services.BuildServiceProvider();
        IInletStore store = provider.GetRequiredService<IInletStore>();

        // Assert
        Assert.IsType<CompositeInletStore>(store);
    }

    /// <summary>
    ///     AddInlet should register IProjectionUpdateNotifier as ProjectionNotifier.
    /// </summary>
    [Fact]
    public void AddInletRegistersIProjectionUpdateNotifierAsProjectionNotifier()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiClientBuilder builder = new(services);

        // Act
        builder.AddInletClient();
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionUpdateNotifier notifier = provider.GetRequiredService<IProjectionUpdateNotifier>();

        // Assert
        Assert.IsType<ProjectionNotifier>(notifier);
    }

    /// <summary>
    ///     AddInlet should register IStore as singleton.
    /// </summary>
    [Fact]
    public void AddInletRegistersIStoreAsSingleton()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiClientBuilder builder = new(services);

        // Act
        builder.AddInletClient();
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
    public void AddInletRegistersIStoreAsStore()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiClientBuilder builder = new(services);

        // Act
        builder.AddInletClient();
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();

        // Assert
        Assert.IsType<Store>(store);
    }

    /// <summary>
    ///     AddInlet should register IProjectionRegistry.
    /// </summary>
    [Fact]
    public void AddInletRegistersProjectionRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiClientBuilder builder = new(services);

        // Act
        builder.AddInletClient();
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
    public void AddInletWithNullServicesThrowsArgumentNullException()
    {
        // Arrange
        IMississippiClientBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddInletClient());
    }

    /// <summary>
    ///     AddProjectionPath should register path in IConfigureProjectionRegistry.
    /// </summary>
    [Fact]
    public void AddProjectionPathRegistersConfiguration()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiClientBuilder builder = new(services);

        // Act
        builder.AddInletClient();
        builder.AddProjectionPath<TestProjection>("cascade/test");
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IConfigureProjectionRegistry> configs = provider.GetServices<IConfigureProjectionRegistry>();

        // Assert
        Assert.Single(configs);
    }

    /// <summary>
    ///     AddProjectionPath should throw ArgumentNullException when path is null.
    /// </summary>
    [Fact]
    public void AddProjectionPathWithNullPathThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiClientBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => { builder.AddProjectionPath<TestProjection>(null!); });
    }

    /// <summary>
    ///     AddProjectionPath should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddProjectionPathWithNullServicesThrowsArgumentNullException()
    {
        // Arrange
        IMississippiClientBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddProjectionPath<TestProjection>("cascade/test"));
    }

    /// <summary>
    ///     AddProjectionPath configuration should register path in registry.
    /// </summary>
    [Fact]
    public void ProjectionPathConfigurationRegistersPathInRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiClientBuilder builder = new(services);
        builder.AddInletClient();
        builder.AddProjectionPath<TestProjection>("cascade/test");
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