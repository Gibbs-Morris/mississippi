using System;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Gateway.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="InletInProcessRegistrations" />.
/// </summary>
public sealed class InletInProcessRegistrationsTests
{
    /// <summary>
    ///     UseInletInProcess can be called multiple times without error.
    /// </summary>
    [Fact]
    public void UseInletInProcessCanBeCalledMultipleTimes()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();

        // Act
        services.UseInletInProcess();
        IServiceCollection result = services.UseInletInProcess();

        // Assert
        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IServerProjectionNotifier>());
    }

    /// <summary>
    ///     UseInletInProcess should register InProcessProjectionNotifier as singleton.
    /// </summary>
    [Fact]
    public void UseInletInProcessRegistersAsSingleton()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        services.UseInletInProcess();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IServerProjectionNotifier notifier1 = provider.GetRequiredService<IServerProjectionNotifier>();
        IServerProjectionNotifier notifier2 = provider.GetRequiredService<IServerProjectionNotifier>();

        // Assert
        Assert.Same(notifier1, notifier2);
    }

    /// <summary>
    ///     UseInletInProcess should register IServerProjectionNotifier.
    /// </summary>
    [Fact]
    public void UseInletInProcessRegistersIServerProjectionNotifier()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        services.UseInletInProcess();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        IServerProjectionNotifier notifier = provider.GetRequiredService<IServerProjectionNotifier>();

        // Assert
        Assert.NotNull(notifier);
    }

    /// <summary>
    ///     UseInletInProcess should return the same services collection for chaining.
    /// </summary>
    [Fact]
    public void UseInletInProcessReturnsSameCollection()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        IServiceCollection result = services.UseInletInProcess();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     UseInletInProcess should throw when services is null.
    /// </summary>
    [Fact]
    public void UseInletInProcessThrowsWhenServicesNull()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => services.UseInletInProcess());
        Assert.Equal("services", exception.ParamName);
    }
}