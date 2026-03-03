using System;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Core;

/// <summary>
///     Extension methods for configuring reducers and effects on an <see cref="IFeatureStateBuilder{TState}" />.
/// </summary>
public static class FeatureStateBuilderExtensions
{
    /// <summary>
    ///     Adds a state-scoped action effect implementation to the feature state.
    /// </summary>
    /// <typeparam name="TEffect">The effect implementation type.</typeparam>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="builder">The feature state builder.</param>
    /// <returns>The same feature state builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder" /> is null.
    /// </exception>
    public static IFeatureStateBuilder<TState> AddActionEffect<TEffect, TState>(
        this IFeatureStateBuilder<TState> builder
    )
        where TEffect : class, IActionEffect<TState>
        where TState : class, IFeatureState, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddActionEffect<TState, TEffect>();
        return builder;
    }

    /// <summary>
    ///     Adds a delegate-based action reducer to the feature state.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="builder">The feature state builder.</param>
    /// <param name="reducer">The delegate invoked to apply actions to the current state.</param>
    /// <returns>The same feature state builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder" /> or <paramref name="reducer" /> is null.
    /// </exception>
    /// <remarks>
    ///     Delegates to
    ///     <see
    ///         cref="ReservoirRegistrations.AddReducer{TAction,TState}(Microsoft.Extensions.DependencyInjection.IServiceCollection, Func{TState,TAction,TState})" />
    ///     on the builder's service collection. Each call registers an additional reducer (not idempotent).
    /// </remarks>
    public static IFeatureStateBuilder<TState> AddReducer<TAction, TState>(
        this IFeatureStateBuilder<TState> builder,
        Func<TState, TAction, TState> reducer
    )
        where TAction : IAction
        where TState : class, IFeatureState, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(reducer);
        builder.Services.AddReducer<TAction, TState>(reducer);
        ((FeatureStateBuilder<TState>)builder).IncrementReducerCount();
        return builder;
    }

    /// <summary>
    ///     Adds a type-based action reducer to the feature state.
    /// </summary>
    /// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <param name="builder">The feature state builder.</param>
    /// <returns>The same feature state builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder" /> is null.
    /// </exception>
    public static IFeatureStateBuilder<TState> AddReducer<TAction, TReducer, TState>(
        this IFeatureStateBuilder<TState> builder
    )
        where TAction : IAction
        where TReducer : class, IActionReducer<TAction, TState>
        where TState : class, IFeatureState, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddReducer<TAction, TState, TReducer>();
        ((FeatureStateBuilder<TState>)builder).IncrementReducerCount();
        return builder;
    }
}