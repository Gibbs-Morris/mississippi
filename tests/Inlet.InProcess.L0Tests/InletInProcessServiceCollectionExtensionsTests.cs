using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet.InProcess.L0Tests;

/// <summary>
///     Tests for <see cref="InletInProcessServiceCollectionExtensions" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.InProcess")]
[AllureSuite("Extensions")]
[AllureSubSuite("InletInProcessServiceCollectionExtensions")]
public sealed class InletInProcessServiceCollectionExtensionsTests
{
    /// <summary>
    ///     AddInletInProcess can be called multiple times without error.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletInProcessCanBeCalledMultipleTimes()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();

        // Act
        services.AddInletInProcess();
        IServiceCollection result = services.AddInletInProcess();

        // Assert
        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IServerProjectionNotifier>());
    }

    /// <summary>
    ///     AddInletInProcess should register InProcessProjectionNotifier as singleton.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletInProcessRegistersAsSingleton()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        services.AddInletInProcess();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IServerProjectionNotifier notifier1 = provider.GetRequiredService<IServerProjectionNotifier>();
        IServerProjectionNotifier notifier2 = provider.GetRequiredService<IServerProjectionNotifier>();

        // Assert
        Assert.Same(notifier1, notifier2);
    }

    /// <summary>
    ///     AddInletInProcess should register IServerProjectionNotifier.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletInProcessRegistersIServerProjectionNotifier()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        services.AddInletInProcess();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IServerProjectionNotifier notifier = provider.GetRequiredService<IServerProjectionNotifier>();

        // Assert
        Assert.NotNull(notifier);
    }

    /// <summary>
    ///     AddInletInProcess should return the same services collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletInProcessReturnsSameCollection()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        IServiceCollection result = services.AddInletInProcess();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletInProcess should throw when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void AddInletInProcessThrowsWhenServicesNull()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => services.AddInletInProcess());
        Assert.Equal("services", exception.ParamName);
    }
}