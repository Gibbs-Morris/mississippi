using System;

using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Extension methods for selecting derived state from the store.
/// </summary>
/// <remarks>
///     <para>
///         Selectors are pure functions that derive values from feature state.
///         They provide a consistent, reusable, and testable way to compute
///         values from the store without duplicating logic across components.
///     </para>
///     <para>
///         Selectors MUST be pure functions with no side effects.
///         For performance-sensitive scenarios, use memoization via
///         <c>Memoize.Create</c> to cache results when the state reference is unchanged.
///     </para>
/// </remarks>
public static class SelectorExtensions
{
    /// <summary>
    ///     Selects a derived value from a feature state using a selector function.
    /// </summary>
    /// <typeparam name="TState">The feature state type.</typeparam>
    /// <typeparam name="TResult">The type of the derived value.</typeparam>
    /// <param name="store">The store to select from.</param>
    /// <param name="selector">
    ///     A pure function that derives a value from the feature state.
    ///     MUST be side-effect free and return the same output for the same input.
    /// </param>
    /// <returns>The derived value.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="store" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static TResult Select<TState, TResult>(
        this IStore store,
        Func<TState, TResult> selector
    )
        where TState : class, IFeatureState
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        TState state = store.GetState<TState>();
        return selector(state);
    }

    /// <summary>
    ///     Selects a derived value by combining multiple feature states.
    /// </summary>
    /// <typeparam name="TState1">The first feature state type.</typeparam>
    /// <typeparam name="TState2">The second feature state type.</typeparam>
    /// <typeparam name="TResult">The type of the derived value.</typeparam>
    /// <param name="store">The store to select from.</param>
    /// <param name="selector">
    ///     A pure function that derives a value from both feature states.
    ///     MUST be side-effect free and return the same output for the same inputs.
    /// </param>
    /// <returns>The derived value.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="store" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static TResult Select<TState1, TState2, TResult>(
        this IStore store,
        Func<TState1, TState2, TResult> selector
    )
        where TState1 : class, IFeatureState
        where TState2 : class, IFeatureState
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        TState1 state1 = store.GetState<TState1>();
        TState2 state2 = store.GetState<TState2>();
        return selector(state1, state2);
    }

    /// <summary>
    ///     Selects a derived value by combining three feature states.
    /// </summary>
    /// <typeparam name="TState1">The first feature state type.</typeparam>
    /// <typeparam name="TState2">The second feature state type.</typeparam>
    /// <typeparam name="TState3">The third feature state type.</typeparam>
    /// <typeparam name="TResult">The type of the derived value.</typeparam>
    /// <param name="store">The store to select from.</param>
    /// <param name="selector">
    ///     A pure function that derives a value from all three feature states.
    ///     MUST be side-effect free and return the same output for the same inputs.
    /// </param>
    /// <returns>The derived value.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="store" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static TResult Select<TState1, TState2, TState3, TResult>(
        this IStore store,
        Func<TState1, TState2, TState3, TResult> selector
    )
        where TState1 : class, IFeatureState
        where TState2 : class, IFeatureState
        where TState3 : class, IFeatureState
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);

        TState1 state1 = store.GetState<TState1>();
        TState2 state2 = store.GetState<TState2>();
        TState3 state3 = store.GetState<TState3>();
        return selector(state1, state2, state3);
    }
}
