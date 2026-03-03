using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.SignalRConnection;


namespace Mississippi.Inlet.Client.L0Tests.Registrations;

/// <summary>
///     Tests for <see cref="SignalRConnectionRegistrations" />.
/// </summary>
public sealed class SignalRConnectionRegistrationsTests
{
    private static readonly MethodInfo AddSignalRConnectionFeatureMethod =
        typeof(SignalRConnectionRegistrations).GetMethod(
            nameof(SignalRConnectionRegistrations.AddSignalRConnectionFeature),
            [typeof(IServiceCollection)]) ??
        throw new InvalidOperationException("Could not locate AddSignalRConnectionFeature registration method.");

    private static IServiceCollection InvokeAddSignalRConnectionFeature(
        IServiceCollection services
    )
    {
        try
        {
            return (IServiceCollection)AddSignalRConnectionFeatureMethod.Invoke(null, [services])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }

    /// <summary>
    ///     AddSignalRConnectionFeature can be called multiple times without error.
    /// </summary>
    [Fact]
    public void AddSignalRConnectionFeatureCanBeCalledMultipleTimes()
    {
        // Arrange
        ServiceCollection services = new();

        // Act (should not throw)
        InvokeAddSignalRConnectionFeature(services);
        InvokeAddSignalRConnectionFeature(services);

        // Assert - just verify no exception was thrown
        Assert.True(services.Count > 0);
    }

    /// <summary>
    ///     AddSignalRConnectionFeature registers all reducers.
    /// </summary>
    [Fact]
    public void AddSignalRConnectionFeatureRegistersAllReducers()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        InvokeAddSignalRConnectionFeature(services);

        // Assert - verify that reducers were registered by checking service count increased
        // Each reducer registration adds at least one service
        Assert.True(services.Count >= 6, "Expected at least 6 services for 6 reducers");
    }

    /// <summary>
    ///     AddSignalRConnectionFeature returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddSignalRConnectionFeatureReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = InvokeAddSignalRConnectionFeature(services);

        // Assert
        Assert.Same(services, result);
    }
}