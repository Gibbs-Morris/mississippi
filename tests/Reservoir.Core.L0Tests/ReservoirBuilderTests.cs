using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core.L0Tests;

/// <summary>
///     Tests for <see cref="ReservoirBuilderExtensions" /> and <see cref="FeatureStateBuilderExtensions" />.
/// </summary>
public sealed class ReservoirBuilderTests
{
    /// <summary>
    ///     Test action effect implementation used in builder tests.
    /// </summary>
    private sealed class CounterEffect : IActionEffect<CounterState>
    {
        /// <inheritdoc />
        public bool CanHandle(
            IAction action
        ) =>
            false;

        /// <inheritdoc />
#pragma warning disable CS1998 // Async method lacks 'await' operators
        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            CounterState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.CompletedTask;
            yield break;
        }
#pragma warning restore CS1998
    }

    /// <summary>
    ///     Test feature state used in builder tests.
    /// </summary>
    private sealed record CounterState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "counter";

        /// <summary>
        ///     Gets the count value.
        /// </summary>
        public int Count { get; init; }
    }

    /// <summary>
    ///     Second test action used in builder tests.
    /// </summary>
    private sealed record DecrementAction : IAction;

    /// <summary>
    ///     Test action used in builder tests.
    /// </summary>
    private sealed record IncrementAction : IAction;

    /// <summary>
    ///     Test type-based reducer implementation.
    /// </summary>
    private sealed class IncrementReducer : ActionReducerBase<IncrementAction, CounterState>
    {
        /// <inheritdoc />
        public override CounterState Reduce(
            CounterState state,
            IncrementAction action
        ) =>
            state with
            {
                Count = state.Count + 1,
            };
    }

    /// <summary>
    ///     Second test feature state.
    /// </summary>
    private sealed record SecondState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "second";

        /// <summary>
        ///     Gets the value.
        /// </summary>
        public int Value { get; init; }
    }

    /// <summary>
    ///     AddActionEffect with null builder should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddActionEffectWithNullBuilderThrowsArgumentNullException()
    {
        // Arrange
        IFeatureStateBuilder<CounterState>? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddActionEffect<CounterEffect, CounterState>());
    }

    /// <summary>
    ///     AddFeature should return builder for fluent chaining.
    /// </summary>
    [Fact]
    public void AddFeatureReturnsBuilderForFluentChaining()
    {
        // Arrange
        ServiceCollection services = [];
        IReservoirBuilder? capturedBuilder = null;
        IReservoirBuilder? returnedBuilder = null;

        // Act
        services.AddReservoir(reservoir =>
        {
            capturedBuilder = reservoir;
            returnedBuilder = reservoir.AddFeature<CounterState>(feature =>
            {
                feature.AddReducer<IncrementAction, CounterState>((
                    state,
                    _
                ) => state);
            });
        });

        // Assert
        Assert.Same(capturedBuilder, returnedBuilder);
    }

    /// <summary>
    ///     AddFeature with action effect should register effect in DI.
    /// </summary>
    [Fact]
    public void AddFeatureWithActionEffectRegistersEffectInDI()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddReservoir(reservoir =>
        {
            reservoir.AddFeature<CounterState>(feature =>
            {
                feature.AddReducer<IncrementAction, CounterState>((
                    state,
                    _
                ) => state with
                {
                    Count = state.Count + 1,
                });
                feature.AddActionEffect<CounterEffect, CounterState>();
            });
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IActionEffect<CounterState>? effect = provider.GetService<IActionEffect<CounterState>>();
        Assert.NotNull(effect);
    }

    /// <summary>
    ///     AddFeature with delegate reducer should register reducer in DI.
    /// </summary>
    [Fact]
    public void AddFeatureWithDelegateReducerRegistersReducerInDI()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddReservoir(reservoir =>
        {
            reservoir.AddFeature<CounterState>(feature =>
            {
                feature.AddReducer<IncrementAction, CounterState>((
                    state,
                    _
                ) => state with
                {
                    Count = state.Count + 1,
                });
            });
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IActionReducer<CounterState>? reducer = provider.GetService<IActionReducer<CounterState>>();
        Assert.NotNull(reducer);
    }

    /// <summary>
    ///     AddFeature with multiple reducers should register all reducers in DI.
    /// </summary>
    [Fact]
    public void AddFeatureWithMultipleReducersRegistersAllReducersInDI()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddReservoir(reservoir =>
        {
            reservoir.AddFeature<CounterState>(feature =>
            {
                feature.AddReducer<IncrementAction, CounterState>((
                    state,
                    _
                ) => state with
                {
                    Count = state.Count + 1,
                });
                feature.AddReducer<DecrementAction, CounterState>((
                    state,
                    _
                ) => state with
                {
                    Count = state.Count - 1,
                });
            });
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IEnumerable<IActionReducer<CounterState>> reducers = provider.GetServices<IActionReducer<CounterState>>();
        Assert.Equal(2, Enumerable.Count(reducers));
    }

    /// <summary>
    ///     AddFeature with no reducers should throw BuilderValidationException.
    /// </summary>
    [Fact]
    public void AddFeatureWithNoReducersThrowsValidationException()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            services.AddReservoir(reservoir => { reservoir.AddFeature<CounterState>(_ => { }); }));
        Assert.Single(exception.Diagnostics);
        Assert.Equal("Reservoir.Feature.NoReducersConfigured", exception.Diagnostics[0].ErrorCode);
    }

    /// <summary>
    ///     AddFeature with null builder should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddFeatureWithNullBuilderThrowsArgumentNullException()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddFeature<CounterState>(_ => { }));
    }

    /// <summary>
    ///     AddFeature with null configure should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddFeatureWithNullConfigureThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddReservoir(reservoir =>
        {
            reservoir.AddFeature<CounterState>(null!);
        }));
    }

    /// <summary>
    ///     AddFeature with type-based reducer should register reducer in DI.
    /// </summary>
    [Fact]
    public void AddFeatureWithTypeBasedReducerRegistersReducerInDI()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddReservoir(reservoir =>
        {
            reservoir.AddFeature<CounterState>(feature =>
            {
                feature.AddReducer<IncrementAction, IncrementReducer, CounterState>();
            });
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IActionReducer<CounterState>? reducer = provider.GetService<IActionReducer<CounterState>>();
        Assert.NotNull(reducer);
    }

    /// <summary>
    ///     AddReducer should return feature builder for fluent chaining.
    /// </summary>
    [Fact]
    public void AddReducerReturnsFeatureBuilderForFluentChaining()
    {
        // Arrange
        ServiceCollection services = [];
        IFeatureStateBuilder<CounterState>? capturedBuilder = null;
        IFeatureStateBuilder<CounterState>? returnedBuilder = null;

        // Act
        services.AddReservoir(reservoir =>
        {
            reservoir.AddFeature<CounterState>(feature =>
            {
                capturedBuilder = feature;
                returnedBuilder = feature.AddReducer<IncrementAction, CounterState>((
                    state,
                    _
                ) => state);
            });
        });

        // Assert
        Assert.Same(capturedBuilder, returnedBuilder);
    }

    /// <summary>
    ///     AddReducer with null builder should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddReducerWithNullBuilderThrowsArgumentNullException()
    {
        // Arrange
        IFeatureStateBuilder<CounterState>? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddReducer<IncrementAction, CounterState>((
            state,
            _
        ) => state));
    }

    /// <summary>
    ///     AddReducer with null reducer should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddReducerWithNullReducerThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddReservoir(reservoir =>
        {
            reservoir.AddFeature<CounterState>(feature =>
            {
                feature.AddReducer<IncrementAction, CounterState>(null!);
            });
        }));
    }

    /// <summary>
    ///     AddReservoir with configure should register IStore and invoke configure delegate.
    /// </summary>
    [Fact]
    public void AddReservoirWithConfigureRegistersStoreAndInvokesDelegate()
    {
        // Arrange
        ServiceCollection services = [];
        bool configureCalled = false;

        // Act
        services.AddReservoir(reservoir =>
        {
            configureCalled = true;
            reservoir.AddFeature<CounterState>(feature =>
            {
                feature.AddReducer<IncrementAction, CounterState>((
                    state,
                    _
                ) => state with
                {
                    Count = state.Count + 1,
                });
            });
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.True(configureCalled);
        Assert.NotNull(provider.GetService<IStore>());
    }

    /// <summary>
    ///     AddReservoir with empty configure should throw BuilderValidationException.
    /// </summary>
    [Fact]
    public void AddReservoirWithEmptyConfigureThrowsValidationException()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert
        BuilderValidationException exception =
            Assert.Throws<BuilderValidationException>(() => services.AddReservoir(_ => { }));
        Assert.Single(exception.Diagnostics);
        Assert.Equal("Reservoir.NoFeaturesConfigured", exception.Diagnostics[0].ErrorCode);
    }

    /// <summary>
    ///     AddReservoir with null configure should skip validation and register store.
    /// </summary>
    [Fact]
    public void AddReservoirWithNullConfigureSkipsValidation()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddReservoir(null);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IStore>());
    }

    /// <summary>
    ///     Feature builder should share parent Services collection.
    /// </summary>
    [Fact]
    public void FeatureBuilderSharesParentServicesCollection()
    {
        // Arrange
        ServiceCollection services = [];
        IServiceCollection? reservoirServices = null;
        IServiceCollection? featureServices = null;

        // Act
        services.AddReservoir(reservoir =>
        {
            reservoirServices = reservoir.Services;
            reservoir.AddFeature<CounterState>(feature =>
            {
                featureServices = feature.Services;
                feature.AddReducer<IncrementAction, CounterState>((
                    state,
                    _
                ) => state);
            });
        });

        // Assert
        Assert.Same(services, reservoirServices);
        Assert.Same(services, featureServices);
    }

    /// <summary>
    ///     Multiple AddFeature calls should register multiple features.
    /// </summary>
    [Fact]
    public void MultipleAddFeatureCallsRegisterMultipleFeatures()
    {
        // Arrange
        ServiceCollection services = [];

        // Act & Assert (should not throw — 2 features configured)
        services.AddReservoir(reservoir =>
        {
            reservoir.AddFeature<CounterState>(feature =>
            {
                feature.AddReducer<IncrementAction, CounterState>((
                    state,
                    _
                ) => state with
                {
                    Count = state.Count + 1,
                });
            });
            reservoir.AddFeature<SecondState>(feature =>
            {
                feature.AddReducer<IncrementAction, SecondState>((
                    state,
                    _
                ) => state with
                {
                    Value = state.Value + 1,
                });
            });
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IStore>());
    }
}