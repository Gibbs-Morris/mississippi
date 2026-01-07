using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

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
    ///     Test action for unit tests.
    /// </summary>
    private sealed record IncrementAction : IAction;

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
    ///     Test feature state for unit tests.
    /// </summary>
    private sealed record TestFeatureState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "test-feature";
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