using System;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.State;


namespace Mississippi.Reservoir.Blazor.L0Tests.BuiltIn.Navigation;

/// <summary>
///     Tests for NavigationFeatureRegistration.
/// </summary>
public sealed class NavigationFeatureRegistrationTests : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    private readonly IStore store;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NavigationFeatureRegistrationTests" /> class.
    /// </summary>
    public NavigationFeatureRegistrationTests()
    {
        ServiceCollection services = [];
        services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        TestMississippiClientBuilder builder = new(services);
        builder.AddReservoir().AddBuiltInNavigation();
        serviceProvider = services.BuildServiceProvider();
        store = serviceProvider.GetRequiredService<IStore>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        store?.Dispose();
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
    ///     AddBuiltInNavigation should register NavigationState feature.
    /// </summary>
    [Fact]
    public void AddBuiltInNavigationRegistersNavigationState()
    {
        // Act
        NavigationState state = store.GetState<NavigationState>();

        // Assert
        Assert.NotNull(state);
        Assert.Null(state.CurrentUri);
        Assert.Equal(0, state.NavigationCount);
    }

    /// <summary>
    ///     AddBuiltInNavigation should register reducer for LocationChangedAction.
    /// </summary>
    [Fact]
    public void AddBuiltInNavigationRegistersReducer()
    {
        // Act
        store.Dispatch(new LocationChangedAction("https://example.com/page", true));

        // Assert
        NavigationState state = store.GetState<NavigationState>();
        Assert.Equal("https://example.com/page", state.CurrentUri);
        Assert.True(state.IsNavigationIntercepted);
        Assert.Equal(1, state.NavigationCount);
    }

    /// <summary>
    ///     AddBuiltInNavigation should return services for chaining.
    /// </summary>
    [Fact]
    public void AddBuiltInNavigationReturnsServicesForChaining()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiClientBuilder builder = new(services);
        IReservoirBuilder reservoirBuilder = builder.AddReservoir();

        // Act
        IReservoirBuilder result = reservoirBuilder.AddBuiltInNavigation();

        // Assert
        Assert.Same(reservoirBuilder, result);
    }

    /// <summary>
    ///     Multiple LocationChangedActions should increment NavigationCount.
    /// </summary>
    [Fact]
    public void MultipleLocationChangedActionsIncrementNavigationCount()
    {
        // Act
        store.Dispatch(new LocationChangedAction("https://example.com/page1", false));
        store.Dispatch(new LocationChangedAction("https://example.com/page2", false));
        store.Dispatch(new LocationChangedAction("https://example.com/page3", false));

        // Assert
        NavigationState state = store.GetState<NavigationState>();
        Assert.Equal(3, state.NavigationCount);
        Assert.Equal("https://example.com/page3", state.CurrentUri);
    }
}