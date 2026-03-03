using System;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.State;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.State;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Client.L0Tests.BuiltIn;

/// <summary>
///     Tests for <see cref="ReservoirBuilderBuiltInExtensions" />.
/// </summary>
public sealed class ReservoirBuilderBuiltInExtensionsTests : IDisposable
{
    private readonly ServiceCollection services;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilderBuiltInExtensionsTests" /> class.
    /// </summary>
    public ReservoirBuilderBuiltInExtensionsTests()
    {
        services = [];
        services.AddSingleton<NavigationManager>(new FakeNavigationManager());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No-op; ServiceProvider disposed per test
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
    ///     AddBuiltIns should register LifecycleState feature.
    /// </summary>
    [Fact]
    public void AddBuiltInsRegistersLifecycleState()
    {
        // Arrange & Act
        services.AddReservoir(reservoir => reservoir.AddBuiltIns());
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();

        // Assert
        LifecycleState state = store.GetState<LifecycleState>();
        Assert.NotNull(state);
        Assert.Equal(LifecyclePhase.NotStarted, state.Phase);
    }

    /// <summary>
    ///     AddBuiltIns should register NavigationState feature.
    /// </summary>
    [Fact]
    public void AddBuiltInsRegistersNavigationState()
    {
        // Arrange & Act
        services.AddReservoir(reservoir => reservoir.AddBuiltIns());
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();

        // Assert
        NavigationState state = store.GetState<NavigationState>();
        Assert.NotNull(state);
    }

    /// <summary>
    ///     AddBuiltIns should return builder for fluent chaining.
    /// </summary>
    [Fact]
    public void AddBuiltInsReturnsBuilderForFluentChaining()
    {
        // Arrange
        IReservoirBuilder? capturedBuilder = null;
        IReservoirBuilder? returnedBuilder = null;

        // Act
        services.AddReservoir(reservoir =>
        {
            capturedBuilder = reservoir;
            returnedBuilder = reservoir.AddBuiltIns();
        });

        // Assert
        Assert.Same(capturedBuilder, returnedBuilder);
    }

    /// <summary>
    ///     AddBuiltIns with null builder should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddBuiltInsWithNullBuilderThrowsArgumentNullException()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddBuiltIns());
    }

    /// <summary>
    ///     AddDevTools should register DevTools service descriptors.
    /// </summary>
    [Fact]
    public void AddDevToolsRegistersDevToolsServices()
    {
        // Arrange & Act
        services.AddReservoir(reservoir =>
        {
            reservoir.AddNavigationFeature();
            reservoir.AddDevTools();
        });

        // Assert — verify the service descriptor was registered (not resolved, to avoid full DI graph)
        Assert.Contains(services, sd => sd.ServiceType == typeof(ReduxDevToolsService));
    }

    /// <summary>
    ///     AddDevTools with configure should apply options.
    /// </summary>
    [Fact]
    public void AddDevToolsWithConfigureAppliesOptions()
    {
        // Arrange & Act
        services.AddReservoir(reservoir =>
        {
            reservoir.AddNavigationFeature();
            reservoir.AddDevTools(options =>
            {
                options.Name = "TestApp";
                options.Enablement = ReservoirDevToolsEnablement.Always;
            });
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<ReservoirDevToolsOptions> options = provider.GetRequiredService<IOptions<ReservoirDevToolsOptions>>();

        // Assert
        Assert.Equal("TestApp", options.Value.Name);
        Assert.Equal(ReservoirDevToolsEnablement.Always, options.Value.Enablement);
    }

    /// <summary>
    ///     AddDevTools with null builder should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddDevToolsWithNullBuilderThrowsArgumentNullException()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddDevTools());
    }

    /// <summary>
    ///     AddLifecycleFeature should register LifecycleState feature only.
    /// </summary>
    [Fact]
    public void AddLifecycleFeatureRegistersLifecycleState()
    {
        // Arrange & Act
        services.AddReservoir(reservoir => reservoir.AddLifecycleFeature());
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();

        // Assert
        LifecycleState state = store.GetState<LifecycleState>();
        Assert.NotNull(state);
        Assert.Equal(LifecyclePhase.NotStarted, state.Phase);
    }

    /// <summary>
    ///     AddLifecycleFeature with null builder should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddLifecycleFeatureWithNullBuilderThrowsArgumentNullException()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddLifecycleFeature());
    }

    /// <summary>
    ///     AddNavigationFeature should register NavigationState feature only.
    /// </summary>
    [Fact]
    public void AddNavigationFeatureRegistersNavigationState()
    {
        // Arrange & Act
        services.AddReservoir(reservoir => reservoir.AddNavigationFeature());
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();

        // Assert
        NavigationState state = store.GetState<NavigationState>();
        Assert.NotNull(state);
        Assert.Null(state.CurrentUri);
        Assert.Null(state.PreviousUri);
        Assert.Equal(0, state.NavigationCount);
    }

    /// <summary>
    ///     AddNavigationFeature with null builder should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddNavigationFeatureWithNullBuilderThrowsArgumentNullException()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddNavigationFeature());
    }
}