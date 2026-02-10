using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Inlet.Server.Abstractions;


namespace Mississippi.Inlet.Blazor.Server.L0Tests;

/// <summary>
///     Tests for <see cref="InletInProcessRegistrations" />.
/// </summary>
public sealed class InletInProcessRegistrationsTests
{
    /// <summary>
    ///     AddInletInProcess can be called multiple times without error.
    /// </summary>
    [Fact]
    public void AddInletInProcessCanBeCalledMultipleTimes()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        TestMississippiServerBuilder builder = new(services);

        // Act
        builder.AddInletInProcess();
        IMississippiServerBuilder result = builder.AddInletInProcess();

        // Assert
        Assert.Same(builder, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IServerProjectionNotifier>());
    }

    /// <summary>
    ///     AddInletInProcess should register InProcessProjectionNotifier as singleton.
    /// </summary>
    [Fact]
    public void AddInletInProcessRegistersAsSingleton()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        TestMississippiServerBuilder builder = new(services);
        builder.AddInletInProcess();

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
    public void AddInletInProcessRegistersIServerProjectionNotifier()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        TestMississippiServerBuilder builder = new(services);
        builder.AddInletInProcess();

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
    public void AddInletInProcessReturnsSameCollection()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiServerBuilder builder = new(services);

        // Act
        IMississippiServerBuilder result = builder.AddInletInProcess();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddInletInProcess should throw when services is null.
    /// </summary>
    [Fact]
    public void AddInletInProcessThrowsWhenServicesNull()
    {
        // Arrange
        IMississippiServerBuilder? builder = null;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => builder!.AddInletInProcess());
        Assert.Equal("builder", exception.ParamName);
    }
}