using System;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Defines the builder used to configure a single Reservoir feature state.
/// </summary>
/// <typeparam name="TState">The feature state type being configured.</typeparam>
/// <remarks>
///     Justification: public contract for feature-level composition over reducers and action effects.
/// </remarks>
public interface IReservoirFeatureBuilder<TState>
    where TState : class, IFeatureState, new()
{
    /// <summary>
    ///     Gets the service collection used for advanced extension scenarios.
    ///     During feature configuration this collection may be a staged view whose mutations are committed
    ///     back to the host builder after the callback completes successfully.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    IServiceCollection Services { get; }

    /// <summary>
    ///     Adds an action effect implementation for the current feature state.
    /// </summary>
    /// <typeparam name="TEffect">The action effect implementation type.</typeparam>
    /// <returns>The feature builder for chaining.</returns>
    IReservoirFeatureBuilder<TState> AddActionEffect<TEffect>()
        where TEffect : class, IActionEffect<TState>;

    /// <summary>
    ///     Adds a reducer expressed as a delegate for the current feature state.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
    /// <param name="reduce">The reduction delegate.</param>
    /// <returns>The feature builder for chaining.</returns>
    IReservoirFeatureBuilder<TState> AddReducer<TAction>(
        Func<TState, TAction, TState> reduce
    )
        where TAction : IAction;

    /// <summary>
    ///     Adds a reducer implementation for the current feature state.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <returns>The feature builder for chaining.</returns>
    IReservoirFeatureBuilder<TState> AddReducer<TAction, TReducer>()
        where TAction : IAction
        where TReducer : class, IActionReducer<TAction, TState>;
}