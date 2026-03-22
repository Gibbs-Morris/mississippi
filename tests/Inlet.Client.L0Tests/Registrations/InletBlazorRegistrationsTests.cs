using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Inlet.Client.L0Tests.Registrations;

/// <summary>
///     Tests for <see cref="InletBlazorRegistrations" />.
/// </summary>
public sealed class InletBlazorRegistrationsTests
{
    /// <summary>
    ///     AddInletBlazor returns the builder for chaining.
    /// </summary>
    [Fact]
    public void AddInletBlazorReturnsBuilder()
    {
        // Arrange
        ServiceCollection services = new();
        IReservoirBuilder builder = services.AddReservoir();

        // Act
        IReservoirBuilder result = builder.AddInletBlazor();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR accepts null configure action and returns the builder for chaining.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRAcceptsNullConfigureAndReturnsBuilder()
    {
        // Arrange
        ServiceCollection services = new();
        IReservoirBuilder builder = services.AddReservoir();

        // Act (should not throw with null/missing configure action)
        IReservoirBuilder result = builder.AddInletBlazorSignalR();

        // Assert - returns the same collection for chaining
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR invokes configure action when provided.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRInvokesConfigureAction()
    {
        // Arrange
        ServiceCollection services = new();
        IReservoirBuilder builder = services.AddReservoir();
        bool configureWasCalled = false;

        // Act
        IReservoirBuilder result = builder.AddInletBlazorSignalR(_ => { configureWasCalled = true; });

        // Assert - action was invoked and returns same collection
        Assert.True(configureWasCalled);
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR registers the core Inlet client services when they are not already present.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRRegistersCoreInletClientServices()
    {
        // Arrange
        ServiceCollection services = new();
        IReservoirBuilder builder = services.AddReservoir();

        // Act
        builder.AddInletBlazorSignalR();

        // Assert
        Assert.Contains(services, sd => sd.ServiceType == typeof(IInletStore));
        Assert.Contains(services, sd => sd.ServiceType == typeof(IProjectionUpdateNotifier));
        Assert.Contains(services, sd => sd.ServiceType == typeof(IProjectionRegistry));
    }

    /// <summary>
    ///     AddInletBlazorSignalR throws ArgumentNullException when builder is null.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRThrowsWhenBuilderIsNull()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddInletBlazorSignalR());
    }

    /// <summary>
    ///     AddInletBlazor throws ArgumentNullException when builder is null.
    /// </summary>
    [Fact]
    public void AddInletBlazorThrowsWhenBuilderIsNull()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddInletBlazor());
    }
}