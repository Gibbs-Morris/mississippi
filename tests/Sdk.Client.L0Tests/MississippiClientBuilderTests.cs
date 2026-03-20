using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Sdk.Client;


namespace MississippiTests.Sdk.Client.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiClientBuilder" />.
/// </summary>
public sealed class MississippiClientBuilderTests
{
    /// <summary>
    ///     AddInletBlazorSignalR should invoke configure and build services.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRInvokesConfigureAndBuildsServices()
    {
        // Arrange
        ServiceCollection services = [];
        MississippiClientBuilder sut = new(services);
        bool configureInvoked = false;

        // Act
        sut.AddInletBlazorSignalR(signalR =>
        {
            configureInvoked = true;
            signalR.WithHubPath("/hubs/inlet");
        });

        // Assert — configure was invoked and services were registered.
        Assert.True(configureInvoked);
        Assert.NotEmpty(services);
    }

    /// <summary>
    ///     AddInletBlazorSignalR with null configure should throw.
    /// </summary>
    [Fact]
    public void AddInletBlazorSignalRWithNullConfigureThrows()
    {
        // Arrange
        ServiceCollection services = [];
        MississippiClientBuilder sut = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.AddInletBlazorSignalR(null!));
    }

    /// <summary>
    ///     AddInletClient should be idempotent and register Inlet services.
    /// </summary>
    [Fact]
    public void AddInletClientIsIdempotentAndRegistersServices()
    {
        // Arrange
        ServiceCollection services = [];
        MississippiClientBuilder sut = new(services);

        // Act — call twice; should not throw and should not duplicate registrations.
        sut.AddInletClient();
        sut.AddInletClient();

        // Assert — services are registered (TryAdd means no duplicates).
        Assert.Contains(services, d => d.ServiceType == typeof(IProjectionRegistry));
        Assert.Contains(services, d => d.ServiceType == typeof(IInletStore));
        Assert.Contains(services, d => d.ServiceType == typeof(IProjectionUpdateNotifier));

        // Assert idempotent — each type registered exactly once.
        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(IProjectionRegistry)));
        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(IInletStore)));
        Assert.Equal(1, services.Count(d => d.ServiceType == typeof(IProjectionUpdateNotifier)));
    }

    /// <summary>
    ///     Constructor with null services should throw.
    /// </summary>
    [Fact]
    public void ConstructorWithNullServicesThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MississippiClientBuilder(null!));
    }

    /// <summary>
    ///     Empty builder should validate successfully.
    /// </summary>
    [Fact]
    public void EmptyBuilderValidatesSuccessfully()
    {
        // Arrange
        ServiceCollection services = [];
        MississippiClientBuilder sut = new(services);

        // Act — should not throw.
        sut.Validate();
    }

    /// <summary>
    ///     GetServices should return the service collection.
    /// </summary>
    [Fact]
    public void GetServicesReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = [];
        MississippiClientBuilder sut = new(services);

        // Act
        IServiceCollection result = sut.GetServices();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Multiple Reservoir calls should reuse the same builder instance.
    /// </summary>
    [Fact]
    public void MultipleReservoirCallsReuseInstance()
    {
        // Arrange
        ServiceCollection services = [];
        MississippiClientBuilder sut = new(services);
        IReservoirBuilder? first = null;
        IReservoirBuilder? second = null;

        // Act
        sut.Reservoir(r => first = r);
        sut.Reservoir(r => second = r);

        // Assert
        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    /// <summary>
    ///     Reservoir callback should invoke the delegate with a non-null builder.
    /// </summary>
    [Fact]
    public void ReservoirInvokesCallbackWithNonNullBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        MississippiClientBuilder sut = new(services);
        IReservoirBuilder? captured = null;

        // Act
        sut.Reservoir(r => captured = r);

        // Assert
        Assert.NotNull(captured);
    }

    /// <summary>
    ///     Reservoir callback with null delegate should throw.
    /// </summary>
    [Fact]
    public void ReservoirWithNullDelegateThrows()
    {
        // Arrange
        ServiceCollection services = [];
        MississippiClientBuilder sut = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reservoir(null!));
    }
}