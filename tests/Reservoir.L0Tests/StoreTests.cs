using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="Store" />.
/// </summary>
public sealed class StoreTests : IDisposable
{
    private readonly Store sut;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StoreTests" /> class.
    /// </summary>
    public StoreTests() => sut = new();

    /// <inheritdoc />
    public void Dispose()
    {
        sut.Dispose();
    }

    /// <summary>
    ///     Test action for unit tests.
    /// </summary>
    private sealed record IncrementAction : IAction;

    /// <summary>
    ///     Ordered middleware for testing pipeline order.
    /// </summary>
    private sealed class OrderedMiddleware : IMiddleware
    {
        private readonly int order;

        private readonly List<int> orderList;

        public OrderedMiddleware(
            int order,
            List<int> orderList
        )
        {
            this.order = order;
            this.orderList = orderList;
        }

        public void Invoke(
            IAction action,
            Action<IAction> nextAction
        )
        {
            orderList.Add(order);
            nextAction(action);
        }
    }

    /// <summary>
    ///     Action effect that returns additional actions.
    /// </summary>
    private sealed class ReturningActionEffect : IActionEffect<TestFeatureState>
    {
        public bool CanHandle(
            IAction action
        ) =>
            action is IncrementAction;

#pragma warning disable CS1998 // Async method lacks 'await' operators
        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            TestFeatureState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            yield return new SecondaryAction();
        }
#pragma warning restore CS1998
    }

    /// <summary>
    ///     Secondary action for testing action effect returns.
    /// </summary>
    private sealed record SecondaryAction : IAction;

    /// <summary>
    ///     Test action effect for unit tests.
    /// </summary>
    private sealed class TestActionEffect : IActionEffect<TestFeatureState>
    {
        private readonly Action onHandle;

        public TestActionEffect(
            Action onHandle
        ) =>
            this.onHandle = onHandle;

        public bool CanHandle(
            IAction action
        ) =>
            action is IncrementAction;

#pragma warning disable CS1998 // Async method lacks 'await' operators
        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            TestFeatureState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            onHandle();
            yield break;
        }
#pragma warning restore CS1998
    }

    /// <summary>
    ///     Test feature reducer.
    /// </summary>
    private sealed class TestFeatureActionReducer : ActionReducerBase<IncrementAction, TestFeatureState>
    {
        /// <inheritdoc />
        public override TestFeatureState Reduce(
            TestFeatureState state,
            IncrementAction action
        ) =>
            state with
            {
                Counter = state.Counter + 1,
            };
    }

    /// <summary>
    ///     Test feature state for unit tests.
    /// </summary>
    private sealed record TestFeatureState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "test-feature";

        /// <summary>
        ///     Gets the counter value.
        /// </summary>
        public int Counter { get; init; }
    }

    /// <summary>
    ///     Test middleware for unit tests.
    /// </summary>
    private sealed class TestMiddleware : IMiddleware
    {
        private readonly Action onInvoke;

        public TestMiddleware(
            Action onInvoke
        ) =>
            this.onInvoke = onInvoke;

        public void Invoke(
            IAction action,
            Action<IAction> nextAction
        )
        {
            onInvoke();
            nextAction(action);
        }
    }

    /// <summary>
    ///     Action effect that throws an exception.
    /// </summary>
    private sealed class ThrowingActionEffect : IActionEffect<TestFeatureState>
    {
        public bool CanHandle(
            IAction action
        ) =>
            action is IncrementAction;

        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            TestFeatureState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }

    /// <summary>
    ///     Action effect that returns actions should dispatch them.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    public async Task ActionEffectReturnsActionsDispatchesThem()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddActionEffect<TestFeatureState, ReturningActionEffect>();
        services.AddReservoir();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();
        int dispatchCount = 0;
        using IDisposable subscription = store.Subscribe(() => dispatchCount++);

        // Act
        store.Dispatch(new IncrementAction());

        // Assert
        await Task.Delay(100);
        Assert.True(dispatchCount >= 2); // Initial + returned action
    }

    /// <summary>
    ///     Action effect that throws should not break dispatch.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    public async Task ActionEffectThatThrowsDoesNotBreakDispatch()
    {
        // Arrange - use DI to register both effects
        bool secondEffectRan = false;
        ServiceCollection services = [];
        services.AddTransient<IActionEffect<TestFeatureState>, ThrowingActionEffect>();
        services.AddTransient<IActionEffect<TestFeatureState>>(_ => new TestActionEffect(() => secondEffectRan = true));
        services.AddRootActionEffect<TestFeatureState>();
        services.AddFeatureState<TestFeatureState>();
        services.AddReservoir();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();

        // Act
        store.Dispatch(new IncrementAction());

        // Assert
        await Task.Delay(100);
        Assert.True(secondEffectRan);
    }

    /// <summary>
    ///     Store constructor with middleware collection should register middleware.
    /// </summary>
    [Fact]
    public void ConstructorWithMiddlewareCollectionRegistersMiddleware()
    {
        // Arrange
        bool middlewareInvoked = false;
        TestMiddleware middleware = new(() => middlewareInvoked = true);

        // Act
        using Store diStore = new([], [middleware]);
        diStore.Dispatch(new IncrementAction());

        // Assert
        Assert.True(middlewareInvoked);
    }

    /// <summary>
    ///     Store constructor with null feature registrations should throw ArgumentNullException.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ArgumentNullException - constructor throws before returning")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Testing ArgumentNullException - constructor throws before returning")]
    public void ConstructorWithNullFeatureRegistrationsThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Store(null!, []));
    }

    /// <summary>
    ///     Store constructor with null middleware should throw ArgumentNullException.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ArgumentNullException - constructor throws before returning")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Testing ArgumentNullException - constructor throws before returning")]
    public void ConstructorWithNullMiddlewareThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Store([], null!));
    }

    /// <summary>
    ///     Dispatch should throw ObjectDisposedException after disposal.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    public void DispatchAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange
        Store disposedStore = new();
        disposedStore.Dispose();
        IncrementAction action = new();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => disposedStore.Dispatch(action));
    }

    /// <summary>
    ///     Dispatch should throw ArgumentNullException when action is null.
    /// </summary>
    [Fact]
    public void DispatchWithNullActionThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Dispatch(null!));
    }

    /// <summary>
    ///     Dispose should clean up resources and allow multiple calls.
    /// </summary>
    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Act
        sut.Dispose();
        sut.Dispose();

        // Assert - should not throw; verify disposed state
        Assert.Throws<ObjectDisposedException>(() => sut.Dispatch(new IncrementAction()));
    }

    /// <summary>
    ///     Store with feature-scoped action effects via DI should invoke effects on dispatch.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    public async Task FeatureScopedActionEffectsInvokedOnDispatch()
    {
        // Arrange
        bool effectHandled = false;
        ServiceCollection services = [];
        services.AddTransient<IActionEffect<TestFeatureState>>(_ => new TestActionEffect(() => effectHandled = true));
        services.AddRootActionEffect<TestFeatureState>();
        services.AddFeatureState<TestFeatureState>();
        services.AddReservoir();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();

        // Act
        store.Dispatch(new IncrementAction());

        // Assert
        await Task.Delay(100);
        Assert.True(effectHandled);
    }

    /// <summary>
    ///     Feature state registration should register state via constructor.
    /// </summary>
    [Fact]
    public void FeatureStateRegistrationRegistersStateViaConstructor()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddFeatureState<TestFeatureState>();
        services.AddReservoir();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // Act
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();
        TestFeatureState state = store.GetState<TestFeatureState>();

        // Assert
        Assert.NotNull(state);
    }

    /// <summary>
    ///     GetState after dispose should throw ObjectDisposedException.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    public void GetStateAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange
        Store disposedStore = new();
        disposedStore.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => disposedStore.GetState<TestFeatureState>());
    }

    /// <summary>
    ///     GetState should throw InvalidOperationException when state is not registered.
    /// </summary>
    [Fact]
    public void GetStateForUnregisteredFeatureThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sut.GetState<TestFeatureState>());
    }

    /// <summary>
    ///     Middleware pipeline should execute in correct order.
    /// </summary>
    [Fact]
    public void MiddlewarePipelineExecutesInOrder()
    {
        // Arrange
        List<int> order = [];
        sut.RegisterMiddleware(new OrderedMiddleware(1, order));
        sut.RegisterMiddleware(new OrderedMiddleware(2, order));
        sut.RegisterMiddleware(new OrderedMiddleware(3, order));

        // Act
        sut.Dispatch(new IncrementAction());

        // Assert
        Assert.Equal([1, 2, 3], order);
    }

    /// <summary>
    ///     Reducer that handles actions should update state.
    /// </summary>
    [Fact]
    public void ReducerUpdatesState()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddReducer<IncrementAction, TestFeatureState, TestFeatureActionReducer>();
        services.AddReservoir();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore diStore = scope.ServiceProvider.GetRequiredService<IStore>();

        // Act
        diStore.Dispatch(new IncrementAction());
        TestFeatureState state = diStore.GetState<TestFeatureState>();

        // Assert
        Assert.Equal(1, state.Counter);
    }

    /// <summary>
    ///     RegisterMiddleware should add middleware to dispatch pipeline.
    /// </summary>
    [Fact]
    public void RegisterMiddlewareAddsToDispatchPipeline()
    {
        // Arrange
        bool middlewareInvoked = false;
        sut.RegisterMiddleware(new TestMiddleware(() => middlewareInvoked = true));

        // Act
        sut.Dispatch(new IncrementAction());

        // Assert
        Assert.True(middlewareInvoked);
    }

    /// <summary>
    ///     RegisterMiddleware after dispose should throw ObjectDisposedException.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    public void RegisterMiddlewareAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange
        Store disposedStore = new();
        disposedStore.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => disposedStore.RegisterMiddleware(new TestMiddleware(() => { })));
    }

    /// <summary>
    ///     RegisterMiddleware should throw ArgumentNullException when middleware is null.
    /// </summary>
    [Fact]
    public void RegisterMiddlewareWithNullThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.RegisterMiddleware(null!));
    }

    /// <summary>
    ///     Subscribe after dispose should throw ObjectDisposedException.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Testing ObjectDisposedException - Subscribe throws before returning")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    public void SubscribeAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange
        Store disposedStore = new();
        disposedStore.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => disposedStore.Subscribe(() => { }));
    }

    /// <summary>
    ///     Subscribe should return a disposable subscription.
    /// </summary>
    [Fact]
    public void SubscribeReturnsDisposableSubscription()
    {
        // Arrange
        bool notified = false;

        // Act
        using IDisposable subscription = sut.Subscribe(() => notified = true);
        sut.Dispatch(new IncrementAction());

        // Assert
        Assert.True(notified);
    }

    /// <summary>
    ///     Subscribe should throw ArgumentNullException when listener is null.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Testing ArgumentNullException - Subscribe throws before returning")]
    public void SubscribeWithNullListenerThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Subscribe(null!));
    }

    /// <summary>
    ///     Subscription dispose can be called multiple times.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit Dispose behavior requires non-using pattern")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing that double-dispose does not throw")]
    [SuppressMessage(
        "Blocker Code Smell",
        "S2699:Tests should include assertions",
        Justification = "Test verifies no exception is thrown on double-dispose")]
    public void SubscriptionDisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        IDisposable subscription = sut.Subscribe(() => { });

        // Act
        subscription.Dispose();
        subscription.Dispose();

        // Assert - test passes if no exception is thrown
        Assert.True(true);
    }

    /// <summary>
    ///     Unsubscribed listener should not receive notifications.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit Dispose behavior requires non-using pattern")]
    public void UnsubscribedListenerDoesNotReceiveNotifications()
    {
        // Arrange
        int callCount = 0;
        IDisposable subscription = sut.Subscribe(() => callCount++);

        // Act
        sut.Dispatch(new IncrementAction());
        subscription.Dispose();
        sut.Dispatch(new IncrementAction());

        // Assert
        Assert.Equal(1, callCount);
    }
}