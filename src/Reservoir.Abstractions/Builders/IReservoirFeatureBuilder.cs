using System;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions.Builders;

/// <summary>
///     Builder contract for Reservoir feature registration.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
public interface IReservoirFeatureBuilder<TState>
    where TState : class, IFeatureState, new()
{
    /// <summary>
    ///     Adds an action effect implementation for the feature.
    /// </summary>
    /// <typeparam name="TEffect">The effect implementation type.</typeparam>
    /// <returns>The feature builder for chaining.</returns>
    IReservoirFeatureBuilder<TState> AddActionEffect<TEffect>()
        where TEffect : class, IActionEffect<TState>;

    /// <summary>
    ///     Adds a reducer expressed as a delegate for the feature.
    /// </summary>
    /// <typeparam name="TAction">The action type.</typeparam>
    /// <param name="reduce">The reducer delegate.</param>
    /// <returns>The feature builder for chaining.</returns>
    IReservoirFeatureBuilder<TState> AddReducer<TAction>(
        Func<TState, TAction, TState> reduce
    )
        where TAction : IAction;

    /// <summary>
    ///     Adds a reducer implementation for the feature.
    /// </summary>
    /// <typeparam name="TAction">The action type.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <returns>The feature builder for chaining.</returns>
    IReservoirFeatureBuilder<TState> AddReducer<TAction, TReducer>()
        where TAction : IAction
        where TReducer : class, IActionReducer<TAction, TState>;

    /// <summary>
    ///     Returns to the parent Reservoir builder.
    /// </summary>
    /// <returns>The Reservoir builder.</returns>
    IReservoirBuilder Done();
}
