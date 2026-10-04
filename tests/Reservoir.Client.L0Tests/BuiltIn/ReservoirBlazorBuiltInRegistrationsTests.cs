using System;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client.BuiltIn;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.State;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.State;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Client.L0Tests.BuiltIn;

/// <summary>
///     Tests for ReservoirBlazorBuiltInRegistrations.
/// </summary>
public sealed class ReservoirBlazorBuiltInRegistrationsTests : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBlazorBuiltInRegistrationsTests" /> class.
    /// </summary>
    public ReservoirBlazorBuiltInRegistrationsTests()
    {
        ServiceCollection services = [];
        services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        IReservoirBuilder builder = services.AddReservoir();
        builder.AddReservoirBlazorBuiltIns();
        serviceProvider = services.BuildServiceProvider();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        serviceProvider.Dispose();
    }

    /// <summary>
    ///     A fake navigation manager for testing purposes.
    /// </summary>
    private sealed class FakeNavigationManager : NavigationManager
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FakeNavigationManager" /> class.
        /// </summary>
        public FakeNavigationManager()
        {
            Initialize("https://example.com/", "https://example.com/");
        }

        /// <inheritdoc />
        protected override void NavigateToCore(
            string uri,
            bool forceLoad
        )
        {
            // No-op for testing
        }

        /// <inheritdoc />
        protected override void NavigateToCore(
            string uri,
            NavigationOptions options
        )
        {
            // No-op for testing
        }
    }

    /// <summary>
    ///     AddReservoirBlazorBuiltIns should register LifecycleState feature.
    /// </summary>
    [Fact]
    public void AddReservoirBlazorBuiltInsRegistersLifecycleState()
    {
        // Arrange
        IStore store = serviceProvider.GetRequiredService<IStore>();

        // Act
        LifecycleState state = store.GetState<LifecycleState>();

        // Assert
        Assert.NotNull(state);
        Assert.Equal(LifecyclePhase.NotStarted, state.Phase);
        Assert.Null(state.InitializedAt);
        Assert.Null(state.ReadyAt);
    }

    /// <summary>
    ///     AddReservoirBlazorBuiltIns should register NavigationState feature.
    /// </summary>
    [Fact]
    public void AddReservoirBlazorBuiltInsRegistersNavigationState()
    {
        // Arrange
        IStore store = serviceProvider.GetRequiredService<IStore>();

        // Act
        NavigationState state = store.GetState<NavigationState>();

        // Assert
        Assert.NotNull(state);
        Assert.Null(state.CurrentUri);
        Assert.Null(state.PreviousUri);
        Assert.Equal(0, state.NavigationCount);
        Assert.False(state.IsNavigationIntercepted);
    }

    /// <summary>
    ///     AddReservoirBlazorBuiltIns should return services for chaining.
    /// </summary>
    [Fact]
    public void AddReservoirBlazorBuiltInsReturnsServicesForChaining()
    {
        // Arrange
        ServiceCollection services = [];
        IReservoirBuilder builder = services.AddReservoir();

        // Act
        IReservoirBuilder result = builder.AddReservoirBlazorBuiltIns();

        // Assert
        Assert.Same(builder, result);
    }
}