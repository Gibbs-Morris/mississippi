#pragma warning disable CS0618 // Testing legacy composition APIs pending issue #237.
using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;
using Mississippi.Reservoir.Core;


namespace Mississippi.Inlet.Client.L0Tests.Registrations;

/// <summary>
///     Tests for <see cref="InletBlazorRegistrations" />.
/// </summary>
public sealed class InletBlazorRegistrationsTests
{
    private sealed class TestFeatureState : IFeatureState
    {
        public static string FeatureKey => nameof(TestFeatureState);
    }

    /// <summary>
    ///     AddInletBlazor on ReservoirBuilder returns the builder for chaining.
    /// </summary>
    [Fact]
    public void AddInletBlazorOnReservoirBuilderReturnsBuilder()
    {
        // Arrange
        ServiceCollection services = new();
        ReservoirBuilder reservoir = new(services);
        reservoir.AddFeature<TestFeatureState>();

        // Act
        IReservoirBuilder result = reservoir.AddInletBlazor();

        // Assert
        Assert.Same(reservoir, result);
    }

    /// <summary>
    ///     AddInletBlazor returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddInletBlazorReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddInletBlazor();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR accepts null configure action and returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRAcceptsNullConfigureAndReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act (should not throw with null/missing configure action)
        IServiceCollection result = services.AddInletBlazorSignalR();

        // Assert - returns the same collection for chaining
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR invokes configure action when provided.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRInvokesConfigureAction()
    {
        // Arrange
        ServiceCollection services = new();
        bool configureWasCalled = false;

        // Act
        IServiceCollection result = services.AddInletBlazorSignalR(builder => { configureWasCalled = true; });

        // Assert - action was invoked and returns same collection
        Assert.True(configureWasCalled);
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR on ReservoirBuilder accepts null configure action and returns the builder.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalROnReservoirBuilderAcceptsNullConfigureAndReturnsBuilder()
    {
        // Arrange
        ServiceCollection services = new();
        ReservoirBuilder reservoir = new(services);
        reservoir.AddFeature<TestFeatureState>();

        // Act
        IReservoirBuilder result = reservoir.AddInletBlazorSignalR();

        // Assert
        Assert.Same(reservoir, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddInletBlazorSignalR());
    }

    /// <summary>
    ///     AddInletBlazor throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddInletBlazorThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddInletBlazor());
    }
}

#pragma warning restore CS0618