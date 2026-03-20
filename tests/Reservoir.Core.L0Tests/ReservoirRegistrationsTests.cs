using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core.L0Tests;

/// <summary>
///     Tests for Reservoir builder-based service composition.
/// </summary>
public sealed class ReservoirRegistrationsTests
{
    /// <summary>
    ///     Test action for unit tests.
    /// </summary>
    private sealed record TestAction : IAction;

    /// <summary>
    ///     Test action effect implementation.
    /// </summary>
    private sealed class TestActionEffect : IActionEffect<TestFeatureState>
    {
        /// <inheritdoc />
        public bool CanHandle(
            IAction action
        ) =>
            action is TestAction;

#pragma warning disable CS1998 // Async method lacks 'await' operators
        /// <inheritdoc />
        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            TestFeatureState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            yield break;
        }
#pragma warning restore CS1998
    }

    /// <summary>
    ///     Test reducer implementation.
    /// </summary>
    private sealed class TestActionReducer : ActionReducerBase<TestAction, TestFeatureState>
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
    ///     Test middleware implementation.
    /// </summary>
    private sealed class TestMiddleware : IMiddleware
    {
        /// <inheritdoc />
        public void Invoke(
            IAction action,
            Action<IAction> nextAction
        ) =>
            nextAction(action);
    }

    /// <summary>
    ///     AddActionEffect should register a feature-scoped action effect in DI.
    /// </summary>
    [Fact]
    public void AddActionEffectRegistersFeatureScopedEffectInDI()
    {
        // Arrange
        ServiceCollection services = [];
        ReservoirBuilder reservoirBuilder = new(services);

        // Act
        reservoirBuilder.AddFeature<TestFeatureState>(feature =>
            feature.AddActionEffect<TestFeatureState, TestActionEffect>());
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IActionEffect<TestFeatureState>> effects = provider.GetServices<IActionEffect<TestFeatureState>>();

        // Assert
        Assert.Single(effects);
    }

    /// <summary>
    ///     AddMiddleware should register middleware in DI.
    /// </summary>
    [Fact]
    public void AddMiddlewareRegistersMiddlewareInDI()
    {
        // Arrange
        ServiceCollection services = [];
        ReservoirBuilder reservoirBuilder = new(services);

        // Act
        reservoirBuilder.AddMiddleware<TestMiddleware>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IMiddleware> middlewares = provider.GetServices<IMiddleware>();

        // Assert
        Assert.Single(middlewares);
    }

    /// <summary>
    ///     AddReducer with delegate should register a reducer in DI.
    /// </summary>
    [Fact]
    public void AddReducerWithDelegateRegistersReducerInDI()
    {
        // Arrange
        ServiceCollection services = [];
        ReservoirBuilder reservoirBuilder = new(services);

        // Act
        reservoirBuilder.AddFeature<TestFeatureState>(feature => feature.AddReducer<TestFeatureState, TestAction>((
            state,
            _
        ) => state with
        {
            Counter = state.Counter + 1,
        }));
        using ServiceProvider provider = services.BuildServiceProvider();
        IActionReducer<TestFeatureState>? reducer = provider.GetService<IActionReducer<TestFeatureState>>();

        // Assert
        Assert.NotNull(reducer);
    }

    /// <summary>
    ///     AddReducer with null delegate should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddReducerWithNullDelegateThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = [];
        ReservoirBuilder reservoirBuilder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            reservoirBuilder.AddFeature<TestFeatureState>(feature =>
                feature.AddReducer<TestFeatureState, TestAction>(null!)));
    }

    /// <summary>
    ///     AddReducer with type should register a reducer in DI.
    /// </summary>
    [Fact]
    public void AddReducerWithTypeRegistersReducerInDI()
    {
        // Arrange
        ServiceCollection services = [];
        ReservoirBuilder reservoirBuilder = new(services);

        // Act
        reservoirBuilder.AddFeature<TestFeatureState>(feature =>
            feature.AddReducer<TestFeatureState, TestAction, TestActionReducer>());
        using ServiceProvider provider = services.BuildServiceProvider();
        IActionReducer<TestFeatureState>? reducer = provider.GetService<IActionReducer<TestFeatureState>>();
        IActionReducer<TestAction, TestFeatureState>? typedReducer =
            provider.GetService<IActionReducer<TestAction, TestFeatureState>>();

        // Assert
        Assert.NotNull(reducer);
        Assert.NotNull(typedReducer);
    }

    /// <summary>
    ///     ReservoirBuilder should not replace an existing IStore registration.
    /// </summary>
    [Fact]
    public void ReservoirBuilderDoesNotReplaceExistingStoreRegistration()
    {
        // Arrange
        ServiceCollection services = [];
        _ = new ReservoirBuilder(services); // First registration

        // Act
        _ = new ReservoirBuilder(services); // Second registration should not replace
        ServiceDescriptor[] descriptors = [.. services];
        int storeCount = descriptors.Count(d => d.ServiceType == typeof(IStore));

        // Assert
        Assert.Equal(1, storeCount);
    }

    /// <summary>
    ///     ReservoirBuilder should register IStore as scoped.
    /// </summary>
    [Fact]
    public void ReservoirBuilderRegistersIStoreAsScoped()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        _ = new ReservoirBuilder(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store1 = scope.ServiceProvider.GetRequiredService<IStore>();
        IStore store2 = scope.ServiceProvider.GetRequiredService<IStore>();

        // Assert
        Assert.Same(store1, store2);
    }

    /// <summary>
    ///     ReservoirBuilder should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void ReservoirBuilderWithNullServicesThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ReservoirBuilder(null!));
    }
}