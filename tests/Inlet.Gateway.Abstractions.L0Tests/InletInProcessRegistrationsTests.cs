using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Gateway.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="InletInProcessRegistrations" />.
/// </summary>
public sealed class InletInProcessRegistrationsTests
{
    private static readonly MethodInfo AddInletInProcessMethod =
        typeof(InletInProcessRegistrations).GetMethod(
            nameof(InletInProcessRegistrations.AddInletInProcess),
            [typeof(IServiceCollection)]) ??
        throw new InvalidOperationException("Could not locate AddInletInProcess registration method.");

    private static IServiceCollection InvokeAddInletInProcess(
        IServiceCollection services
    )
    {
        try
        {
            return (IServiceCollection)AddInletInProcessMethod.Invoke(null, [services])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }

    /// <summary>
    ///     AddInletInProcess can be called multiple times without error.
    /// </summary>
    [Fact]
    public void AddInletInProcessCanBeCalledMultipleTimes()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();

        // Act
        InvokeAddInletInProcess(services);
        IServiceCollection result = InvokeAddInletInProcess(services);

        // Assert
        Assert.Same(services, result);
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
        InvokeAddInletInProcess(services);

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
        InvokeAddInletInProcess(services);

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

        // Act
        IServiceCollection result = InvokeAddInletInProcess(services);

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletInProcess should throw when services is null.
    /// </summary>
    [Fact]
    public void AddInletInProcessThrowsWhenServicesNull()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => InvokeAddInletInProcess(services));
        Assert.Equal("services", exception.ParamName);
    }
}