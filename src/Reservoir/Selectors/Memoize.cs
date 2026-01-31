using System;


namespace Mississippi.Reservoir.Selectors;

/// <summary>
///     Utilities for creating memoized selectors that cache results
///     when the input state reference is unchanged.
/// </summary>
/// <remarks>
///     <para>
///         Memoization improves performance for expensive derived computations
///         by returning the cached result when the input has not changed.
///         Since Reservoir uses immutable state with reference semantics,
///         a simple reference equality check is sufficient to detect changes.
///     </para>
///     <para>
///         For selectors combining multiple states, consider composing
///         memoized single-state selectors rather than memoizing the
///         combined selector directly, as this provides finer-grained caching.
///     </para>
/// </remarks>
public static class Memoize
{
    /// <summary>
    ///     Creates a memoized version of a selector that caches the result
    ///     when the input state reference is unchanged.
    /// </summary>
    /// <typeparam name="TState">The feature state type (must be a reference type).</typeparam>
    /// <typeparam name="TResult">The type of the derived value.</typeparam>
    /// <param name="selector">
    ///     The selector function to memoize.
    ///     MUST be a pure function with no side effects.
    /// </param>
    /// <returns>
    ///     A memoized selector that returns the cached result when the state reference is unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="selector" /> is null.
    /// </exception>
    /// <example>
    ///     <code>
    ///     // Create a memoized selector for expensive computation
    ///     var getExpensiveValue = Memoize.Create&lt;MyState, ExpensiveResult&gt;(
    ///         state =&gt; ComputeExpensiveResult(state.Items));
    ///
    ///     // Usage in component
    ///     var result = Select(getExpensiveValue);
    ///     </code>
    /// </example>
    public static Func<TState, TResult> Create<TState, TResult>(
        Func<TState, TResult> selector
    )
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(selector);

        TState? lastInput = null;
        TResult? lastResult = default;

        return state =>
        {
            // Reference equality check - if same reference, return cached result
            if (ReferenceEquals(state, lastInput))
            {
                return lastResult!;
            }

            // State changed, recompute
            lastInput = state;
            lastResult = selector(state);
            return lastResult;
        };
    }

    /// <summary>
    ///     Creates a memoized version of a two-state selector that caches the result
    ///     when both input state references are unchanged.
    /// </summary>
    /// <typeparam name="TState1">The first feature state type.</typeparam>
    /// <typeparam name="TState2">The second feature state type.</typeparam>
    /// <typeparam name="TResult">The type of the derived value.</typeparam>
    /// <param name="selector">
    ///     The selector function to memoize.
    ///     MUST be a pure function with no side effects.
    /// </param>
    /// <returns>
    ///     A memoized selector that returns the cached result when both state references are unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="selector" /> is null.
    /// </exception>
    public static Func<TState1, TState2, TResult> Create<TState1, TState2, TResult>(
        Func<TState1, TState2, TResult> selector
    )
        where TState1 : class
        where TState2 : class
    {
        ArgumentNullException.ThrowIfNull(selector);

        TState1? lastInput1 = null;
        TState2? lastInput2 = null;
        TResult? lastResult = default;

        return (state1, state2) =>
        {
            // Reference equality check for both inputs
            if (ReferenceEquals(state1, lastInput1) && ReferenceEquals(state2, lastInput2))
            {
                return lastResult!;
            }

            // At least one state changed, recompute
            lastInput1 = state1;
            lastInput2 = state2;
            lastResult = selector(state1, state2);
            return lastResult;
        };
    }

    /// <summary>
    ///     Creates a memoized version of a three-state selector that caches the result
    ///     when all input state references are unchanged.
    /// </summary>
    /// <typeparam name="TState1">The first feature state type.</typeparam>
    /// <typeparam name="TState2">The second feature state type.</typeparam>
    /// <typeparam name="TState3">The third feature state type.</typeparam>
    /// <typeparam name="TResult">The type of the derived value.</typeparam>
    /// <param name="selector">
    ///     The selector function to memoize.
    ///     MUST be a pure function with no side effects.
    /// </param>
    /// <returns>
    ///     A memoized selector that returns the cached result when all state references are unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="selector" /> is null.
    /// </exception>
    public static Func<TState1, TState2, TState3, TResult> Create<TState1, TState2, TState3, TResult>(
        Func<TState1, TState2, TState3, TResult> selector
    )
        where TState1 : class
        where TState2 : class
        where TState3 : class
    {
        ArgumentNullException.ThrowIfNull(selector);

        TState1? lastInput1 = null;
        TState2? lastInput2 = null;
        TState3? lastInput3 = null;
        TResult? lastResult = default;

        return (state1, state2, state3) =>
        {
            // Reference equality check for all inputs
            if (ReferenceEquals(state1, lastInput1) &&
                ReferenceEquals(state2, lastInput2) &&
                ReferenceEquals(state3, lastInput3))
            {
                return lastResult!;
            }

            // At least one state changed, recompute
            lastInput1 = state1;
            lastInput2 = state2;
            lastInput3 = state3;
            lastResult = selector(state1, state2, state3);
            return lastResult;
        };
    }
}
