using System;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Implements feature-scoped Reservoir builder operations over an <see cref="IServiceCollection" />.
/// </summary>
/// <typeparam name="TState">The feature state type being configured.</typeparam>
internal sealed class ReservoirFeatureBuilder<TState> : IReservoirFeatureBuilder<TState>
    where TState : class, IFeatureState, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirFeatureBuilder{TState}" /> class.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    public ReservoirFeatureBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    ///     Gets the underlying service collection for advanced extension scenarios.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Adds an action effect implementation for the current feature state.
    /// </summary>
    /// <typeparam name="TEffect">The action effect implementation type.</typeparam>
    /// <returns>The feature builder for chaining.</returns>
    public IReservoirFeatureBuilder<TState> AddActionEffect<TEffect>()
        where TEffect : class, IActionEffect<TState>
    {
        ReservoirBuilderRegistrations.AddActionEffect<TState, TEffect>(Services);
        return this;
    }

    /// <summary>
    ///     Adds a reducer delegate for the current feature state.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
    /// <param name="reduce">The reducer delegate.</param>
    /// <returns>The feature builder for chaining.</returns>
    public IReservoirFeatureBuilder<TState> AddReducer<TAction>(
        Func<TState, TAction, TState> reduce
    )
        where TAction : IAction
    {
        ReservoirBuilderRegistrations.AddReducer<TAction, TState>(Services, reduce);
        return this;
    }

    /// <summary>
    ///     Adds a reducer implementation for the current feature state.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <returns>The feature builder for chaining.</returns>
    public IReservoirFeatureBuilder<TState> AddReducer<TAction, TReducer>()
        where TAction : IAction
        where TReducer : class, IActionReducer<TAction, TState>
    {
        ReservoirBuilderRegistrations.AddReducer<TAction, TState, TReducer>(Services);
        return this;
    }
}