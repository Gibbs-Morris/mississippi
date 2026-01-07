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
    ///     AddInlet with configure should throw ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void AddInletWithNullConfigureThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddInlet(null!));
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
    ///     AddProjectionRoute should register route in IConfigureProjectionRegistry.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddProjectionRouteRegistersConfiguration()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInlet();
        services.AddProjectionRoute<TestProjection>("/api/test");
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IConfigureProjectionRegistry> configs = provider.GetServices<IConfigureProjectionRegistry>();

        // Assert
        Assert.Single(configs);
    }

    /// <summary>
    ///     AddProjectionRoute should throw ArgumentNullException when route is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void AddProjectionRouteWithNullRouteThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddProjectionRoute<TestProjection>(null!));
    }

    /// <summary>
    ///     AddProjectionRoute should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void AddProjectionRouteWithNullServicesThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddProjectionRoute<TestProjection>("/api/test"));
    }
}