using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions.Builders;


namespace Mississippi.Inlet.Client.L0Tests.Registrations;

/// <summary>
///     Tests for <see cref="InletBlazorRegistrations" />.
/// </summary>
public sealed class InletBlazorRegistrationsTests
{
    /// <summary>
    ///     AddInletBlazor returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddInletBlazorReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiClientBuilder mississippiBuilder = new(services);
        IReservoirBuilder reservoirBuilder = mississippiBuilder.AddReservoir();

        // Act
        IReservoirBuilder result = reservoirBuilder.AddInletBlazor();

        // Assert
        Assert.Same(reservoirBuilder, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR accepts null configure action and returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRAcceptsNullConfigureAndReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiClientBuilder mississippiBuilder = new(services);
        IReservoirBuilder reservoirBuilder = mississippiBuilder.AddReservoir();

        // Act (should not throw with null/missing configure action)
        IReservoirBuilder result = reservoirBuilder.AddInletBlazorSignalR();

        // Assert - returns the same collection for chaining
        Assert.Same(reservoirBuilder, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR invokes configure action when provided.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRInvokesConfigureAction()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiClientBuilder mississippiBuilder = new(services);
        IReservoirBuilder reservoirBuilder = mississippiBuilder.AddReservoir();
        bool configureWasCalled = false;

        // Act
        IReservoirBuilder result = reservoirBuilder.AddInletBlazorSignalR(builder => { configureWasCalled = true; });

        // Assert - action was invoked and returns same collection
        Assert.True(configureWasCalled);
        Assert.Same(reservoirBuilder, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRThrowsWhenServicesIsNull()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddInletBlazorSignalR());
    }

    /// <summary>
    ///     AddInletBlazor throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddInletBlazorThrowsWhenServicesIsNull()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddInletBlazor());
    }
}