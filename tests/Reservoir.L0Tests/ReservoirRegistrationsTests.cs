using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="ReservoirRegistrations" />.
/// </summary>
[AllureParentSuite("Mississippi.Reservoir")]
[AllureSuite("Configuration")]
[AllureSubSuite("ReservoirRegistrations")]
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
    ///     AddActionEffect should register feature-scoped action effect in DI.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddActionEffectRegistersFeatureScopedEffectInDI()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddActionEffect<TestFeatureState, TestActionEffect>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IActionEffect<TestFeatureState>> effects = provider.GetServices<IActionEffect<TestFeatureState>>();

        // Assert
        Assert.Single(effects);
    }

    /// <summary>
    ///     AddMiddleware should register middleware in DI.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddMiddlewareRegistersMiddlewareInDI()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddMiddleware<TestMiddleware>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IMiddleware> middlewares = provider.GetServices<IMiddleware>();

        // Assert
        Assert.Single(middlewares);
    }

    /// <summary>
    ///     AddReducer with delegate should register reducer in DI.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddReducerWithDelegateRegistersReducerInDI()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddReducer<TestAction, TestFeatureState>((
            state,
            _
        ) => state with
        {
            Counter = state.Counter + 1,
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        IActionReducer<TestFeatureState>? reducer = provider.GetService<IActionReducer<TestFeatureState>>();

        // Assert
        Assert.NotNull(reducer);
    }

    /// <summary>
    ///     AddReducer with null delegate should throw ArgumentNullException.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void AddReducerWithNullDelegateThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddReducer<TestAction, TestFeatureState>(null!));
    }

    /// <summary>
    ///     AddReducer with type should register reducer in DI.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddReducerWithTypeRegistersReducerInDI()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddReducer<TestAction, TestFeatureState, TestActionReducer>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IActionReducer<TestFeatureState>? reducer = provider.GetService<IActionReducer<TestFeatureState>>();
        IActionReducer<TestAction, TestFeatureState>? typedReducer =
            provider.GetService<IActionReducer<TestAction, TestFeatureState>>();

        // Assert
        Assert.NotNull(reducer);
        Assert.NotNull(typedReducer);
    }

    /// <summary>
    ///     AddReservoir should not replace existing IStore registration.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddReservoirDoesNotReplaceExistingRegistration()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddReservoir(); // First registration

        // Act
        services.AddReservoir(); // Second registration should not replace
        ServiceDescriptor[] descriptors = [.. services];
        int storeCount = descriptors.Count(d => d.ServiceType == typeof(IStore));

        // Assert
        Assert.Equal(1, storeCount);
    }

    /// <summary>
    ///     AddReservoir should register IStore as scoped.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddReservoirRegistersIStoreAsScoped()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddReservoir();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store1 = scope.ServiceProvider.GetRequiredService<IStore>();
        IStore store2 = scope.ServiceProvider.GetRequiredService<IStore>();

        // Assert
        Assert.Same(store1, store2);
    }

    /// <summary>
    ///     AddReservoir should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void AddReservoirWithNullServicesThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddReservoir());
    }

    /// <summary>
    ///     AddRootReducer should register root reducer in DI.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddRootReducerRegistersRootReducerInDI()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddReducer<TestAction, TestFeatureState, TestActionReducer>();

        // Act
        services.AddRootReducer<TestFeatureState>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootReducer<TestFeatureState>? rootReducer = provider.GetService<IRootReducer<TestFeatureState>>();

        // Assert
        Assert.NotNull(rootReducer);
    }
}