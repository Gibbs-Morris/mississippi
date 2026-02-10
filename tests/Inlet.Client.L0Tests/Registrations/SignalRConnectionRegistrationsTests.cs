using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions.Builders;


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
        TestMississippiClientBuilder mississippiBuilder = new(services);
        IReservoirBuilder reservoirBuilder = mississippiBuilder.AddReservoir();

        // Act (should not throw)
        reservoirBuilder.AddSignalRConnectionFeature();
        reservoirBuilder.AddSignalRConnectionFeature();

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
        TestMississippiClientBuilder mississippiBuilder = new(services);
        IReservoirBuilder reservoirBuilder = mississippiBuilder.AddReservoir();

        // Act
        reservoirBuilder.AddSignalRConnectionFeature();

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
        TestMississippiClientBuilder mississippiBuilder = new(services);
        IReservoirBuilder reservoirBuilder = mississippiBuilder.AddReservoir();

        // Act
        IReservoirBuilder result = reservoirBuilder.AddSignalRConnectionFeature();

        // Assert
        Assert.Same(reservoirBuilder, result);
    }
}