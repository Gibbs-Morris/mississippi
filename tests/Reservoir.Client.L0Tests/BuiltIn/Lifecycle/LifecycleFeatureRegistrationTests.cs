using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;


namespace Mississippi.Reservoir.Blazor.L0Tests.BuiltIn.Lifecycle;

/// <summary>
///     Tests for LifecycleFeatureRegistration.
/// </summary>
public sealed class LifecycleFeatureRegistrationTests : IDisposable
{
    private readonly FakeTimeProvider fakeTimeProvider;

    private readonly ServiceProvider serviceProvider;

    private readonly IStore store;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LifecycleFeatureRegistrationTests" /> class.
    /// </summary>
    public LifecycleFeatureRegistrationTests()
    {
        fakeTimeProvider = new();
        fakeTimeProvider.SetUtcNow(new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        ServiceCollection services = [];
        services.AddSingleton<TimeProvider>(fakeTimeProvider);
        services.AddReservoir();
        services.AddBuiltInLifecycle();
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
    ///     AddBuiltInLifecycle should register reducer for AppInitAction.
    /// </summary>
    [Fact]
    public void AddBuiltInLifecycleRegistersAppInitReducer()
    {
        // Arrange
        DateTimeOffset expectedTime = fakeTimeProvider.GetUtcNow();

        // Act
        store.Dispatch(new AppInitAction(expectedTime));

        // Assert
        LifecycleState state = store.GetState<LifecycleState>();
        Assert.Equal(LifecyclePhase.Initializing, state.Phase);
        Assert.Equal(expectedTime, state.InitializedAt);
        Assert.Null(state.ReadyAt);
    }

    /// <summary>
    ///     AddBuiltInLifecycle should register reducer for AppReadyAction.
    /// </summary>
    [Fact]
    public void AddBuiltInLifecycleRegistersAppReadyReducer()
    {
        // Arrange
        DateTimeOffset initTime = fakeTimeProvider.GetUtcNow();
        store.Dispatch(new AppInitAction(initTime));
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(5));
        DateTimeOffset readyTime = fakeTimeProvider.GetUtcNow();

        // Act
        store.Dispatch(new AppReadyAction(readyTime));

        // Assert
        LifecycleState state = store.GetState<LifecycleState>();
        Assert.Equal(LifecyclePhase.Ready, state.Phase);
        Assert.Equal(initTime, state.InitializedAt);
        Assert.Equal(readyTime, state.ReadyAt);
    }

    /// <summary>
    ///     AddBuiltInLifecycle should register LifecycleState feature.
    /// </summary>
    [Fact]
    public void AddBuiltInLifecycleRegistersLifecycleState()
    {
        // Act
        LifecycleState state = store.GetState<LifecycleState>();

        // Assert
        Assert.NotNull(state);
        Assert.Equal(LifecyclePhase.NotStarted, state.Phase);
        Assert.Null(state.InitializedAt);
        Assert.Null(state.ReadyAt);
    }

    /// <summary>
    ///     AddBuiltInLifecycle should return services for chaining.
    /// </summary>
    [Fact]
    public void AddBuiltInLifecycleReturnsServicesForChaining()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddReservoir();

        // Act
        IServiceCollection result = services.AddBuiltInLifecycle();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Full lifecycle from NotStarted to Ready should work.
    /// </summary>
    [Fact]
    public void FullLifecycleFlowWorks()
    {
        // Assert initial state
        LifecycleState state = store.GetState<LifecycleState>();
        Assert.Equal(LifecyclePhase.NotStarted, state.Phase);

        // Act - initialize
        DateTimeOffset initTime = fakeTimeProvider.GetUtcNow();
        store.Dispatch(new AppInitAction(initTime));

        // Assert - initializing
        state = store.GetState<LifecycleState>();
        Assert.Equal(LifecyclePhase.Initializing, state.Phase);
        Assert.Equal(initTime, state.InitializedAt);

        // Act - become ready
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(500));
        DateTimeOffset readyTime = fakeTimeProvider.GetUtcNow();
        store.Dispatch(new AppReadyAction(readyTime));

        // Assert - ready
        state = store.GetState<LifecycleState>();
        Assert.Equal(LifecyclePhase.Ready, state.Phase);
        Assert.Equal(initTime, state.InitializedAt);
        Assert.Equal(readyTime, state.ReadyAt);
    }
}