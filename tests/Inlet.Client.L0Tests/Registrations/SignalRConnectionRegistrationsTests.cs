
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.SignalRConnection;


namespace Mississippi.Inlet.Client.L0Tests.Registrations;

/// <summary>
///     Tests for <see cref="SignalRConnectionRegistrations" />.
/// </summary>
public sealed class SignalRConnectionRegistrationsTests
{
    /// <summary>
    ///     AddSignalRConnectionFeature can be called multiple times without error.
    /// </summary>
    [Fact]
        public void AddSignalRConnectionFeatureCanBeCalledMultipleTimes()
    {
        // Arrange
        ServiceCollection services = new();

        // Act (should not throw)
        services.AddSignalRConnectionFeature();
        services.AddSignalRConnectionFeature();

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
        services.AddSignalRConnectionFeature();

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
        IServiceCollection result = services.AddSignalRConnectionFeature();

        // Assert
        Assert.Same(services, result);
    }
}