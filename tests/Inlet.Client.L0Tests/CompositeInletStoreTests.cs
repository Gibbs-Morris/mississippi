using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Events;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for <see cref="CompositeInletStore" />.
/// </summary>
public sealed class CompositeInletStoreTests : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    private readonly Store store;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CompositeInletStoreTests" /> class.
    /// </summary>
    public CompositeInletStoreTests()
    {
        // Set up DI container with Store and ProjectionsFeatureState
        ServiceCollection services = new();
        services.AddReservoir();
        services.AddFeatureState<ProjectionsFeatureState>();
        serviceProvider = services.BuildServiceProvider();
        store = (Store)serviceProvider.GetRequiredService<IStore>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        store.Dispose();
        serviceProvider.Dispose();
    }

    /// <summary>
    ///     Simple observer for testing observable subscriptions.
    /// </summary>
    /// <typeparam name="T">The type of observed values.</typeparam>
    private sealed class ActionObserver<T> : IObserver<T>
    {
        private readonly Action<T> onNext;

        public ActionObserver(
            Action<T> onNext
        ) =>
            this.onNext = onNext;

        public void OnCompleted()
        {
        }

        public void OnError(
            Exception error
        )
        {
        }

        public void OnNext(
            T value
        ) =>
            onNext(value);
    }

    /// <summary>
    ///     Middleware for capturing dispatched actions.
    /// </summary>
    private sealed class CaptureMiddleware : IMiddleware
    {
        private readonly Action<IAction> capture;

        public CaptureMiddleware(
            Action<IAction> capture
        ) =>
            this.capture = capture;

        public void Invoke(
            IAction action,
            Action<IAction> nextAction
        )
        {
            capture(action);
            nextAction(action);
        }
    }

    /// <summary>
    ///     Test action for dispatch tests.
    /// </summary>
    private sealed record TestAction : IAction;

    /// <summary>
    ///     Constructor should throw when store is null.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Assert.Throws catches the exception before any object is created")]
    public void ConstructorThrowsWhenStoreIsNull() =>
        Assert.Throws<ArgumentNullException>(() => new CompositeInletStore(null!));

    /// <summary>
    ///     Dispatch should delegate to underlying store.
    /// </summary>
    [Fact]
    public void DispatchDelegatesToStore()
    {
        // Arrange
        IAction? capturedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => capturedAction = a));
        using CompositeInletStore sut = new(store);
        TestAction action = new();

        // Act
        sut.Dispatch(action);

        // Assert
        Assert.Same(action, capturedAction);
    }

    /// <summary>
    ///     Dispose should be callable multiple times without error.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown on multiple dispose calls")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Intentionally testing multiple dispose calls")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit dispose pattern")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing explicit dispose pattern - resources are disposed in the test")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Objects are explicitly disposed as part of the test")]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange - create a new isolated store for this test
        using ServiceProvider localServiceProvider = new ServiceCollection().AddReservoir()
            .AddFeatureState<ProjectionsFeatureState>()
            .BuildServiceProvider();
        Store localStore = (Store)localServiceProvider.GetRequiredService<IStore>();
        CompositeInletStore sut = new(localStore);

        // Act - call dispose twice
        sut.Dispose();
        sut.Dispose();

        // Assert - no exception thrown
    }

    /// <summary>
    ///     GetState should delegate to underlying store.
    /// </summary>
    [Fact]
    public void GetStateDelegatesToStore()
    {
        // Arrange
        using CompositeInletStore sut = new(store);

        // Act
        ProjectionsFeatureState state = sut.GetState<ProjectionsFeatureState>();

        // Assert
        Assert.NotNull(state);
    }

    /// <summary>
    ///     GetStateSnapshot should delegate to underlying store.
    /// </summary>
    [Fact]
    public void GetStateSnapshotDelegatesToStore()
    {
        // Arrange
        using CompositeInletStore sut = new(store);

        // Act
        IReadOnlyDictionary<string, object> snapshot = sut.GetStateSnapshot();

        // Assert
        Assert.NotNull(snapshot);
        Assert.True(snapshot.ContainsKey(ProjectionsFeatureState.FeatureKey));
    }

    /// <summary>
    ///     StoreEvents should delegate to underlying store.
    /// </summary>
    [Fact]
    public void StoreEventsDelegatesToStore()
    {
        // Arrange
        using CompositeInletStore sut = new(store);
        int eventCount = 0;
        using IDisposable subscription =
            sut.StoreEvents.Subscribe(new ActionObserver<StoreEventBase>(_ => eventCount++));

        // Act
        sut.Dispatch(new TestAction());

        // Assert - should receive ActionDispatchingEvent and ActionDispatchedEvent
        Assert.True(eventCount >= 2);
    }

    /// <summary>
    ///     Subscribe should delegate to underlying store.
    /// </summary>
    [Fact]
    public void SubscribeDelegatesToStore()
    {
        // Arrange
        using CompositeInletStore sut = new(store);
        int callCount = 0;

        // Act
        using IDisposable subscription = sut.Subscribe(() => callCount++);
        sut.Dispatch(new TestAction());

        // Assert
        Assert.Equal(1, callCount);
    }
}