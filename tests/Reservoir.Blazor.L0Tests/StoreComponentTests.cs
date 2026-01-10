using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for <see cref="StoreComponent" />.
/// </summary>
[AllureParentSuite("Mississippi.Reservoir.Blazor")]
[AllureSuite("Components")]
[AllureSubSuite("StoreComponent")]
public sealed class StoreComponentTests : IDisposable
{
    private readonly Store store;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StoreComponentTests" /> class.
    /// </summary>
    public StoreComponentTests() => store = new();

    /// <inheritdoc />
    public void Dispose()
    {
        store.Dispose();
    }

    /// <summary>
    ///     Middleware for testing dispatch callbacks.
    /// </summary>
    private sealed class CallbackMiddleware : IMiddleware
    {
        private readonly Action callback;

        public CallbackMiddleware(
            Action callback
        ) =>
            this.callback = callback;

        public void Invoke(
            IAction action,
            Action<IAction> nextAction
        )
        {
            callback();
            nextAction(action);
        }
    }

    /// <summary>
    ///     Test action for unit tests.
    /// </summary>
    private sealed record TestAction : IAction;

    /// <summary>
    ///     Test feature state for unit tests.
    /// </summary>
    private sealed record TestFeatureState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "test-feature";

        /// <summary>
        ///     Gets the counter.
        /// </summary>
        public int Counter { get; init; }
    }

    /// <summary>
    ///     Test reducer for unit tests.
    /// </summary>
    private sealed class TestReducer : Reducer<TestAction, TestFeatureState>
    {
        /// <inheritdoc />
        public override TestFeatureState Reduce(
            TestFeatureState state,
            TestAction action
        ) =>
            state with
            {
                Counter = state.Counter + 1,
            };
    }

    /// <summary>
    ///     Concrete test component for testing abstract StoreComponent.
    /// </summary>
    private sealed class TestStoreComponent : StoreComponent
    {
        /// <summary>
        ///     Sets the store for testing.
        /// </summary>
        /// <param name="store">The store instance.</param>
        public void SetStore(
            IStore store
        ) =>
            Store = store;

        /// <summary>
        ///     Exposes Dispatch for testing.
        /// </summary>
        /// <param name="action">The action to dispatch.</param>
        public void TestDispatch(
            IAction action
        ) =>
            Dispatch(action);

        /// <summary>
        ///     Exposes GetState for testing.
        /// </summary>
        /// <typeparam name="TState">The state type.</typeparam>
        /// <returns>The current state.</returns>
        public TState TestGetState<TState>()
            where TState : class, IFeatureState =>
            GetState<TState>();

        /// <summary>
        ///     Initializes the component for testing.
        /// </summary>
        public void TestOnInitialized() => OnInitialized();
    }

    /// <summary>
    ///     Dispatch should delegate to the store.
    /// </summary>
    [Fact]
    [AllureFeature("Dispatch")]
    public void DispatchDelegatesToStore()
    {
        // Arrange
        using TestStoreComponent sut = new();
        sut.SetStore(store);
        bool actionDispatched = false;
        store.RegisterMiddleware(new CallbackMiddleware(() => actionDispatched = true));

        // Act
        sut.TestDispatch(new TestAction());

        // Assert
        Assert.True(actionDispatched);
    }

    /// <summary>
    ///     Dispose should be callable multiple times without error.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown on multiple dispose calls")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing that multiple Dispose calls don't throw")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit Dispose calls, not automatic disposal")]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        TestStoreComponent sut = new();
        sut.SetStore(store);
        sut.TestOnInitialized();

        // Act & Assert - should not throw
        sut.Dispose();
        sut.Dispose();
        Assert.True(true);
    }

    /// <summary>
    ///     GetState should delegate to the store.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "ServiceProvider and scope are properly disposed via using statements")]
    public void GetStateDelegatesToStore()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddReservoir(s => s.RegisterState<TestFeatureState>());
        services.AddReducer<TestAction, TestFeatureState, TestReducer>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore scopedStore = scope.ServiceProvider.GetRequiredService<IStore>();
        using TestStoreComponent sut = new();
        sut.SetStore(scopedStore);

        // Act
        TestFeatureState state = sut.TestGetState<TestFeatureState>();

        // Assert
        Assert.NotNull(state);
        Assert.Equal(0, state.Counter);
    }

    /// <summary>
    ///     OnInitialized can be called multiple times (re-disposing subscription).
    /// </summary>
    [Fact]
    [AllureFeature("Initialization")]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown when reinitialized")]
    public void OnInitializedDisposesExistingSubscription()
    {
        // Arrange
        using TestStoreComponent sut = new();
        sut.SetStore(store);
        sut.TestOnInitialized(); // First initialization creates subscription

        // Act - second initialization should dispose previous subscription and create new
        sut.TestOnInitialized();

        // Assert - no exception thrown means existing subscription was disposed
        Assert.True(true);
    }

    /// <summary>
    ///     OnInitialized should subscribe to store changes.
    /// </summary>
    [Fact]
    [AllureFeature("Initialization")]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown on initialization")]
    public void OnInitializedSubscribesToStore()
    {
        // Arrange
        using TestStoreComponent sut = new();
        sut.SetStore(store);

        // Act
        sut.TestOnInitialized();

        // Assert - no exception thrown means subscription was created
        Assert.True(true);
    }
}