using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;
using Mississippi.Reservoir.Core.State;


namespace Mississippi.Reservoir.Core.L0Tests;

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
    ///     Root reducer for test feature state that handles IncrementAction.
    /// </summary>
    private sealed class TestFeatureRootReducer : IRootReducer<TestFeatureState>
    {
        private readonly RootReducer<TestFeatureState> innerReducer = new([new TestFeatureActionReducer()]);

        /// <inheritdoc />
        public TestFeatureState Reduce(
            TestFeatureState state,
            IAction action
        ) =>
            innerReducer.Reduce(state, action);
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
        IReservoirBuilder builder = services.AddReservoir();
        builder.AddFeatureState<TestFeatureState>(feature => feature.AddActionEffect<ReturningActionEffect>());
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();
        int dispatchCount = 0;
        using IDisposable subscription = store.Subscribe(() => dispatchCount++);

        // Act
        store.Dispatch(new IncrementAction());

        // Assert
        await Task.Delay(100, TestContext.Current.CancellationToken);
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
        services.AddTransient<IRootActionEffect<TestFeatureState>, RootActionEffect<TestFeatureState>>();
        IReservoirBuilder builder = services.AddReservoir();
        builder.AddFeatureState<TestFeatureState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();

        // Act
        store.Dispatch(new IncrementAction());

        // Assert
        await Task.Delay(100, TestContext.Current.CancellationToken);
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
        using Store diStore = new([], [middleware], TimeProvider.System);
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
        Assert.Throws<ArgumentNullException>(() => new Store(null!, [], TimeProvider.System));
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
        Assert.Throws<ArgumentNullException>(() => new Store([], null!, TimeProvider.System));
    }

    /// <summary>
    ///     Critical listener failures are propagated as the original instance instead of being isolated.
    /// </summary>
    /// <param name="exceptionType">The critical listener exception type.</param>
    [Theory]
    [InlineData(typeof(OutOfMemoryException))]
    [InlineData(typeof(StackOverflowException))]
    [InlineData(typeof(AccessViolationException))]
    [InlineData(typeof(ThreadInterruptedException))]
    public void CriticalListenerFailurePropagates(
        Type exceptionType
    )
    {
        Exception failure = Assert.IsType<Exception>(Activator.CreateInstance(exceptionType), false);
        ExceptionDispatchInfo captured = ExceptionDispatchInfo.Capture(failure);
        int laterCalls = 0;
        using IDisposable failed = sut.Subscribe(captured.Throw);
        using IDisposable later = sut.Subscribe(() => laterCalls++);
        Exception actual = Assert.Throws(exceptionType, () => sut.Dispatch(new IncrementAction()));
        Assert.Same(failure, actual);
        Assert.Equal(0, laterCalls);
    }

    /// <summary>
    ///     Critical logger failures propagate as the original instance instead of being isolated.
    /// </summary>
    /// <param name="exceptionType">The critical logging exception type.</param>
    /// <param name="throwFromIsEnabled">Whether the logger fails during its enabled check.</param>
    [Theory]
    [InlineData(typeof(OutOfMemoryException), false)]
    [InlineData(typeof(OutOfMemoryException), true)]
    [InlineData(typeof(StackOverflowException), false)]
    [InlineData(typeof(StackOverflowException), true)]
    [InlineData(typeof(AccessViolationException), false)]
    [InlineData(typeof(AccessViolationException), true)]
    [InlineData(typeof(ThreadInterruptedException), false)]
    [InlineData(typeof(ThreadInterruptedException), true)]
    public void CriticalLoggerFailurePropagates(
        Type exceptionType,
        bool throwFromIsEnabled
    )
    {
        Exception failure = Assert.IsType<Exception>(Activator.CreateInstance(exceptionType), false);
        StoreThrowingLogger logger = new(failure, throwFromIsEnabled);
        using Store store = new(TimeProvider.System, logger);
        int laterCalls = 0;
        using IDisposable failed = store.Subscribe(() => throw new InvalidOperationException("Listener failed."));
        using IDisposable later = store.Subscribe(() => laterCalls++);
        Exception actual = Assert.Throws(exceptionType, () => store.Dispatch(new IncrementAction()));
        Assert.Same(failure, actual);
        Assert.Equal(0, laterCalls);
    }

    /// <summary>
    ///     Disabled error logging does not change listener isolation or submit an event.
    /// </summary>
    [Fact]
    public void DisabledLoggingStillAllowsLaterListeners()
    {
        StoreCapturingLogger logger = new(false);
        ServiceCollection services = [];
        services.AddSingleton<ILogger<Store>>(logger);
        services.AddReservoir();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();
        int laterCalls = 0;
        using IDisposable failed = store.Subscribe(() => throw new InvalidOperationException("Listener failed."));
        using IDisposable later = store.Subscribe(() => laterCalls++);
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, laterCalls);
        Assert.Empty(logger.Entries);
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
        services.AddTransient<IRootActionEffect<TestFeatureState>, RootActionEffect<TestFeatureState>>();
        IReservoirBuilder builder = services.AddReservoir();
        builder.AddFeatureState<TestFeatureState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();

        // Act
        store.Dispatch(new IncrementAction());

        // Assert
        await Task.Delay(100, TestContext.Current.CancellationToken);
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
        IReservoirBuilder builder = services.AddReservoir();
        builder.AddFeatureState<TestFeatureState>();
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
    ///     GetStateSnapshot should return the current state of all registered features.
    /// </summary>
    [Fact]
    public void GetStateSnapshotReturnsCurrentState()
    {
        // Arrange - create store with reducer and initial state
        IRootReducer<TestFeatureState> reducer = new TestFeatureRootReducer();
        List<IFeatureStateRegistration> registrations =
        [
            new FeatureStateRegistration<TestFeatureState>(reducer),
        ];
        using Store store = new(registrations, Array.Empty<IMiddleware>(), TimeProvider.System);

        // Act - get initial snapshot
        IReadOnlyDictionary<string, object> initialSnapshot = store.GetStateSnapshot();

        // Assert - initial state should have Counter = 0
        TestFeatureState initialState = (TestFeatureState)initialSnapshot[TestFeatureState.FeatureKey];
        Assert.Equal(0, initialState.Counter);

        // Act - dispatch action and get updated snapshot
        store.Dispatch(new IncrementAction());
        IReadOnlyDictionary<string, object> updatedSnapshot = store.GetStateSnapshot();

        // Assert - snapshot should reflect updated state
        TestFeatureState updatedState = (TestFeatureState)updatedSnapshot[TestFeatureState.FeatureKey];
        Assert.Equal(1, updatedState.Counter);
    }

    /// <summary>
    ///     An interruption of a blocked callback propagates and stops later notification.
    /// </summary>
    [Fact]
    public void InterruptedListenerThreadPropagates()
    {
        using ManualResetEventSlim callbackBlocked = new();
        using ManualResetEventSlim callbackStarted = new();
        Exception? dispatchFailure = null;
        int laterCalls = 0;
        using IDisposable failed = sut.Subscribe(() =>
        {
            callbackStarted.Set();
            callbackBlocked.Wait();
        });
        using IDisposable later = sut.Subscribe(() => Interlocked.Increment(ref laterCalls));
        Thread dispatcher = new(() =>
        {
            try
            {
                sut.Dispatch(new IncrementAction());
            }
            catch (ThreadInterruptedException exception)
            {
                dispatchFailure = exception;
            }
        })
        {
            IsBackground = true,
        };
        dispatcher.Start();
        try
        {
            Assert.True(callbackStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        }
        finally
        {
            if (dispatcher.IsAlive)
            {
                dispatcher.Interrupt();
            }

            Assert.True(dispatcher.Join(TimeSpan.FromSeconds(5)), "Interrupted dispatch thread should exit.");
        }

        Assert.IsType<ThreadInterruptedException>(dispatchFailure);
        Assert.Equal(0, laterCalls);
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
    ///     Ordinary failures in either logger entry point cannot stop later listeners or effects.
    /// </summary>
    /// <param name="throwFromIsEnabled">Whether the logger fails during its enabled check.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryLoggerFailureStillAllowsLaterListenersAndEffects(
        bool throwFromIsEnabled
    )
    {
        StoreThrowingLogger logger = new(new InvalidOperationException("Logger failed."), throwFromIsEnabled);
        TaskCompletionSource effectRan = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ServiceCollection services = [];
        services.AddSingleton<ILogger<Store>>(logger);
        services.AddTransient<IActionEffect<TestFeatureState>>(_ => new TestActionEffect(() => effectRan.SetResult()));
        services.AddTransient<IRootActionEffect<TestFeatureState>, RootActionEffect<TestFeatureState>>();
        services.AddReservoir().AddFeatureState<TestFeatureState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();
        int laterCalls = 0;
        using IDisposable failed = store.Subscribe(() => throw new InvalidOperationException("Listener failed."));
        using IDisposable later = store.Subscribe(() => laterCalls++);
        store.Dispatch(new IncrementAction());
        await effectRan.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, laterCalls);
    }

    /// <summary>
    ///     Reducer that handles actions should update state.
    /// </summary>
    [Fact]
    public void ReducerUpdatesState()
    {
        // Arrange
        ServiceCollection services = [];
        IReservoirBuilder builder = services.AddReservoir();
        builder.AddFeatureState<TestFeatureState>(feature => feature
            .AddReducer<IncrementAction, TestFeatureActionReducer>());
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
    ///     State reset keeps notifying later subscribers after an ordinary subscriber failure.
    /// </summary>
    [Fact]
    public void ResetContinuesPastThrowingListener()
    {
        int laterCalls = 0;
        using IDisposable failed = sut.Subscribe(() => throw new InvalidOperationException("Listener failed."));
        using IDisposable later = sut.Subscribe(() => laterCalls++);
        sut.Dispatch(new ResetToInitialStateAction());
        Assert.Equal(1, laterCalls);
    }

    /// <summary>
    ///     ResetToInitialStateAction should reset state to initial values.
    /// </summary>
    [Fact]
    public void ResetToInitialStateActionResetsState()
    {
        // Arrange - create store with reducer
        IRootReducer<TestFeatureState> reducer = new TestFeatureRootReducer();
        List<IFeatureStateRegistration> registrations =
        [
            new FeatureStateRegistration<TestFeatureState>(reducer),
        ];
        using Store store = new(registrations, Array.Empty<IMiddleware>(), TimeProvider.System);

        // Increment state first
        store.Dispatch(new IncrementAction());
        store.Dispatch(new IncrementAction());
        Assert.Equal(2, store.GetState<TestFeatureState>().Counter);

        // Act
        store.Dispatch(new ResetToInitialStateAction());

        // Assert - should be back to initial state
        TestFeatureState state = store.GetState<TestFeatureState>();
        Assert.Equal(0, state.Counter);
    }

    /// <summary>
    ///     State restoration keeps notifying later subscribers after an ordinary subscriber failure.
    /// </summary>
    [Fact]
    public void RestoreContinuesPastThrowingListener()
    {
        int laterCalls = 0;
        using IDisposable failed = sut.Subscribe(() => throw new InvalidOperationException("Listener failed."));
        using IDisposable later = sut.Subscribe(() => laterCalls++);
        sut.Dispatch(new RestoreStateAction(new Dictionary<string, object>()));
        Assert.Equal(1, laterCalls);
    }

    /// <summary>
    ///     RestoreStateAction should ignore incompatible feature states.
    /// </summary>
    [Fact]
    public void RestoreStateActionIgnoresIncompatibleState()
    {
        // Arrange
        List<IFeatureStateRegistration> registrations =
        [
            new FeatureStateRegistration<TestFeatureState>(),
        ];
        using Store store = new(registrations, Array.Empty<IMiddleware>(), TimeProvider.System);
        IReadOnlyDictionary<string, object> newSnapshot = new Dictionary<string, object>
        {
            [TestFeatureState.FeatureKey] = "not-a-state",
        };

        // Act
        store.Dispatch(new RestoreStateAction(newSnapshot));

        // Assert - state should be unchanged (incompatible type ignored)
        TestFeatureState state = store.GetState<TestFeatureState>();
        Assert.Equal(0, state.Counter);
    }

    /// <summary>
    ///     RestoreStateAction should restore state from snapshot.
    /// </summary>
    [Fact]
    public void RestoreStateActionRestoresStateFromSnapshot()
    {
        // Arrange - create store with reducer
        IRootReducer<TestFeatureState> reducer = new TestFeatureRootReducer();
        List<IFeatureStateRegistration> registrations =
        [
            new FeatureStateRegistration<TestFeatureState>(reducer),
        ];
        using Store store = new(registrations, Array.Empty<IMiddleware>(), TimeProvider.System);

        // Increment state
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, store.GetState<TestFeatureState>().Counter);

        // Create snapshot with different value
        IReadOnlyDictionary<string, object> snapshot = new Dictionary<string, object>
        {
            [TestFeatureState.FeatureKey] = new TestFeatureState
            {
                Counter = 42,
            },
        };

        // Act
        store.Dispatch(new RestoreStateAction(snapshot));

        // Assert
        TestFeatureState state = store.GetState<TestFeatureState>();
        Assert.Equal(42, state.Counter);
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
    ///     A subscriber failure does not block later listeners or the action-effect pipeline, and is logged.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ThrowingListenerDoesNotBlockLaterListenersOrEffects()
    {
        StoreCapturingLogger logger = new();
        InvalidOperationException failure = new("Listener failed.");
        TaskCompletionSource effectRan = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ServiceCollection services = [];
        services.AddSingleton<ILogger<Store>>(logger);
        services.AddTransient<IActionEffect<TestFeatureState>>(_ => new TestActionEffect(() => effectRan.SetResult()));
        services.AddTransient<IRootActionEffect<TestFeatureState>, RootActionEffect<TestFeatureState>>();
        services.AddReservoir().AddFeatureState<TestFeatureState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();
        int laterCalls = 0;
        using IDisposable failed = store.Subscribe(() => throw failure);
        using IDisposable later = store.Subscribe(() => laterCalls++);
        store.Dispatch(new IncrementAction());
        await effectRan.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, laterCalls);
        StoreCapturedLog entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal(new(1, "ListenerFailed"), entry.EventId);
        Assert.Equal("Store listener failed; continuing notification.", entry.Message);
        Assert.Same(failure, entry.Exception);
        KeyValuePair<string, object?> field = Assert.Single(entry.State);
        Assert.Equal("{OriginalFormat}", field.Key);
        Assert.Equal(entry.Message, field.Value);
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