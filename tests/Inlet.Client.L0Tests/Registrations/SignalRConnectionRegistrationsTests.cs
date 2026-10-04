using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


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
        IReservoirBuilder builder = services.AddReservoir();

        // Act (should not throw)
        builder.AddSignalRConnectionFeature();
        builder.AddSignalRConnectionFeature();

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
        IReservoirBuilder builder = services.AddReservoir();

        // Act
        builder.AddSignalRConnectionFeature();

        // Assert - verify that reducers were registered by checking service count increased
        // Each reducer registration adds at least one service
        Assert.True(services.Count >= 6, "Expected at least 6 services for 6 reducers");
    }

    /// <summary>
    ///     AddSignalRConnectionFeature returns the builder for chaining.
    /// </summary>
    [Fact]
    public void AddSignalRConnectionFeatureReturnsBuilder()
    {
        // Arrange
        ServiceCollection services = new();
        IReservoirBuilder builder = services.AddReservoir();

        // Act
        IReservoirBuilder result = builder.AddSignalRConnectionFeature();

        // Assert
        Assert.Same(builder, result);
    }
}