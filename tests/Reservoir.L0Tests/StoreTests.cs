using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="Store" />.
/// </summary>
[AllureParentSuite("Mississippi.Reservoir")]
[AllureSuite("Core")]
[AllureSubSuite("Store")]
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
    ///     Disposable effect for testing disposal.
    /// </summary>
    private sealed class DisposableEffect
        : IEffect,
          IDisposable
    {
        public bool IsDisposed { get; private set; }

        public bool CanHandle(
            IAction action
        ) =>
            false;

        public void Dispose() => IsDisposed = true;

#pragma warning disable CS1998 // Async method lacks 'await' operators
        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            yield break;
        }
#pragma warning restore CS1998
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
    ///     Effect that returns additional actions.
    /// </summary>
    private sealed class ReturningEffect : IEffect
    {
        public bool CanHandle(
            IAction action
        ) =>
            action is IncrementAction;

#pragma warning disable CS1998 // Async method lacks 'await' operators
        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            yield return new SecondaryAction();
        }
#pragma warning restore CS1998
    }

    /// <summary>
    ///     Secondary action for testing effect returns.
    /// </summary>
    private sealed record SecondaryAction : IAction;

    /// <summary>
    ///     Test effect for unit tests.
    /// </summary>
    private sealed class TestEffect : IEffect
    {
        private readonly Action onHandle;

        public TestEffect(
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
    ///     Effect that throws an exception.
    /// </summary>
    private sealed class ThrowingEffect : IEffect
    {
        public bool CanHandle(
            IAction action
        ) =>
            action is IncrementAction;

        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
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
    ///     Store constructor with null service provider should throw ArgumentNullException.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ArgumentNullException - constructor throws before returning")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Testing ArgumentNullException - constructor throws before returning")]
    public void ConstructorWithNullServiceProviderThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Store(null!));
    }

    /// <summary>
    ///     Store constructor with service provider should resolve effects from DI.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    [AllureFeature("DI Integration")]
    public async Task ConstructorWithServiceProviderResolvesEffects()
    {
        // Arrange
        bool effectHandled = false;
        ServiceCollection services = [];
        services.AddTransient<IEffect>(_ => new TestEffect(() => effectHandled = true));
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        using Store diStore = new(provider);
        diStore.Dispatch(new IncrementAction());

        // Assert
        await Task.Delay(100);
        Assert.True(effectHandled);
    }

    /// <summary>
    ///     Store constructor with service provider should resolve middleware from DI.
    /// </summary>
    [Fact]
    [AllureFeature("DI Integration")]
    public void ConstructorWithServiceProviderResolvesMiddleware()
    {
        // Arrange
        bool middlewareInvoked = false;
        ServiceCollection services = [];
        services.AddTransient<IMiddleware>(_ => new TestMiddleware(() => middlewareInvoked = true));
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        using Store diStore = new(provider);
        diStore.Dispatch(new IncrementAction());

        // Assert
        Assert.True(middlewareInvoked);
    }

    /// <summary>
    ///     Dispatch should throw ObjectDisposedException after disposal.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
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
    [AllureFeature("Validation")]
    public void DispatchWithNullActionThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Dispatch(null!));
    }

    /// <summary>
    ///     Dispose should clean up resources and allow multiple calls.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Act
        sut.Dispose();
        sut.Dispose();

        // Assert - should not throw; verify disposed state
        Assert.Throws<ObjectDisposedException>(() => sut.Dispatch(new IncrementAction()));
    }

    /// <summary>
    ///     Dispose should dispose effects that implement IDisposable.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing disposal behavior requires manual disposal")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing disposal behavior requires manual disposal")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Testing disposal behavior - object is disposed in test")]
    public void DisposeDisposesEffects()
    {
        // Arrange
        DisposableEffect effect = new();
        using Store testStore = new();
        testStore.RegisterEffect(effect);

        // Act
        testStore.Dispose();

        // Assert
        Assert.True(effect.IsDisposed);
    }

    /// <summary>
    ///     Effect that returns actions should dispatch them.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    [AllureFeature("Effects")]
    public async Task EffectReturnsActionsDispatchesThem()
    {
        // Arrange
        int dispatchCount = 0;
        sut.RegisterMiddleware(new TestMiddleware(() => dispatchCount++));
        sut.RegisterEffect(new ReturningEffect());

        // Act
        sut.Dispatch(new IncrementAction());

        // Assert
        await Task.Delay(100);
        Assert.True(dispatchCount >= 2); // Initial + returned action
    }

    /// <summary>
    ///     Effect that throws should not break dispatch.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    [AllureFeature("Effects")]
    public async Task EffectThatThrowsDoesNotBreakDispatch()
    {
        // Arrange
        bool secondEffectRan = false;
        sut.RegisterEffect(new ThrowingEffect());
        sut.RegisterEffect(new TestEffect(() => secondEffectRan = true));

        // Act
        sut.Dispatch(new IncrementAction());

        // Assert
        await Task.Delay(100);
        Assert.True(secondEffectRan);
    }

    /// <summary>
    ///     GetState after dispose should throw ObjectDisposedException.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
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
    [AllureFeature("State Retrieval")]
    public void GetStateForUnregisteredFeatureThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sut.GetState<TestFeatureState>());
    }

    /// <summary>
    ///     Middleware pipeline should execute in correct order.
    /// </summary>
    [Fact]
    [AllureFeature("Middleware")]
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
    [AllureFeature("Reduction")]
    public void ReducerUpdatesState()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddTransient<IActionReducer<TestFeatureState>, TestFeatureActionReducer>();
        services.AddTransient<IRootReducer<TestFeatureState>, RootReducer<TestFeatureState>>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using Store diStore = new(provider);
        diStore.RegisterState<TestFeatureState>();

        // Act
        diStore.Dispatch(new IncrementAction());
        TestFeatureState state = diStore.GetState<TestFeatureState>();

        // Assert
        Assert.Equal(1, state.Counter);
    }

    /// <summary>
    ///     RegisterEffect should add effect to store.
    /// </summary>
    /// <returns>A task representing the async test operation.</returns>
    [Fact]
    [AllureFeature("Effects")]
    public async Task RegisterEffectAddsToStore()
    {
        // Arrange
        bool effectHandled = false;
        TestEffect effect = new(() => effectHandled = true);

        // Act
        sut.RegisterEffect(effect);
        sut.Dispatch(new IncrementAction());

        // Assert - wait for async effect with timeout
        await Task.Delay(100);
        Assert.True(effectHandled);
    }

    /// <summary>
    ///     RegisterEffect after dispose should throw ObjectDisposedException.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
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
    public void RegisterEffectAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange
        Store disposedStore = new();
        disposedStore.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => disposedStore.RegisterEffect(new TestEffect(() => { })));
    }

    /// <summary>
    ///     RegisterEffect should throw ArgumentNullException when effect is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void RegisterEffectWithNullThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.RegisterEffect(null!));
    }

    /// <summary>
    ///     RegisterMiddleware should add middleware to dispatch pipeline.
    /// </summary>
    [Fact]
    [AllureFeature("Middleware")]
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
    [AllureFeature("Disposal")]
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
    [AllureFeature("Validation")]
    public void RegisterMiddlewareWithNullThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.RegisterMiddleware(null!));
    }

    /// <summary>
    ///     RegisterState should register feature state from DI.
    /// </summary>
    [Fact]
    [AllureFeature("State Registration")]
    public void RegisterStateRegistersFeatureStateFromDI()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddTransient<IActionReducer<TestFeatureState>, TestFeatureActionReducer>();
        services.AddTransient<IRootReducer<TestFeatureState>, RootReducer<TestFeatureState>>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using Store diStore = new(provider);

        // Act
        diStore.RegisterState<TestFeatureState>();
        TestFeatureState state = diStore.GetState<TestFeatureState>();

        // Assert
        Assert.NotNull(state);
    }

    /// <summary>
    ///     RegisterState without service provider should throw InvalidOperationException.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void RegisterStateWithoutServiceProviderThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sut.RegisterState<TestFeatureState>());
    }

    /// <summary>
    ///     Subscribe after dispose should throw ObjectDisposedException.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
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
    [AllureFeature("Subscriptions")]
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
    [AllureFeature("Validation")]
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
    [AllureFeature("Subscriptions")]
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
    [AllureFeature("Subscriptions")]
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