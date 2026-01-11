using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet.Orleans.L0Tests;

/// <summary>
///     Tests for <see cref="InletOrleansRegistrations" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Orleans")]
[AllureSuite("Extensions")]
[AllureSubSuite("InletOrleansRegistrations")]
public sealed class InletOrleansRegistrationsTests
{
    /// <summary>
    ///     AddInletOrleans can be called multiple times.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletOrleansCanBeCalledMultipleTimes()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddInletOrleans();
        IServiceCollection result = services.AddInletOrleans();

        // Assert
        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IProjectionBrookRegistry>());
    }

    /// <summary>
    ///     AddInletOrleans should register as singleton.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletOrleansRegistersAsSingleton()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddInletOrleans();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry1 = provider.GetRequiredService<IProjectionBrookRegistry>();
        IProjectionBrookRegistry registry2 = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        Assert.Same(registry1, registry2);
    }

    /// <summary>
    ///     AddInletOrleans should register IProjectionBrookRegistry.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletOrleansRegistersIProjectionBrookRegistry()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddInletOrleans();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionBrookRegistry registry = provider.GetRequiredService<IProjectionBrookRegistry>();

        // Assert
        Assert.NotNull(registry);
    }

    /// <summary>
    ///     AddInletOrleans should return the same services collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletOrleansReturnsSameCollection()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        IServiceCollection result = services.AddInletOrleans();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletOrleans should throw when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void AddInletOrleansThrowsWhenServicesNull()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => services.AddInletOrleans());
        Assert.Equal("services", exception.ParamName);
    }

    /// <summary>
    ///     ScanProjectionAssemblies should return the same services collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Assembly Scanning")]
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
    [AllureFeature("Argument Validation")]
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
    [AllureFeature("Argument Validation")]
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
    ///     ScanProjectionAssemblies with no assemblies creates empty registry.
    /// </summary>
    [Fact]
    [AllureFeature("Assembly Scanning")]
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
        Assert.Empty(registry.GetAllProjectionTypes());
    }
}