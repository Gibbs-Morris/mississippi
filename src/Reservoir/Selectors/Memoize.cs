using System;
using System.Threading;


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
///         This implementation is lock-free and thread-safe. Under heavy contention,
///         the selector may be invoked multiple times for the same state (benign race),
///         but results are always correct. This trade-off is acceptable because
///         selectors must be pure functions with no side effects.
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
    public static Func<TState, TResult> Create<TState, TResult>(
        Func<TState, TResult> selector
    )
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(selector);
        CacheEntry<TState, TResult>? cache = null;
        return state =>
        {
            // Volatile read ensures we see the latest cache entry
            CacheEntry<TState, TResult>? current = Volatile.Read(ref cache);

            // Reference equality check - if same reference, return cached result
            if (current is not null && ReferenceEquals(state, current.Input))
            {
                return current.Result;
            }

            // State changed, recompute and update cache atomically via single reference write
            TResult result = selector(state);
            Volatile.Write(ref cache, new(state, result));
            return result;
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
        CacheEntry2<TState1, TState2, TResult>? cache = null;
        return (
            state1,
            state2
        ) =>
        {
            // Volatile read ensures we see the latest cache entry
            CacheEntry2<TState1, TState2, TResult>? current = Volatile.Read(ref cache);

            // Reference equality check for both inputs
            if (current is not null &&
                ReferenceEquals(state1, current.Input1) &&
                ReferenceEquals(state2, current.Input2))
            {
                return current.Result;
            }

            // At least one state changed, recompute and update cache atomically
            TResult result = selector(state1, state2);
            Volatile.Write(ref cache, new(state1, state2, result));
            return result;
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
        CacheEntry3<TState1, TState2, TState3, TResult>? cache = null;
        return (
            state1,
            state2,
            state3
        ) =>
        {
            // Volatile read ensures we see the latest cache entry
            CacheEntry3<TState1, TState2, TState3, TResult>? current = Volatile.Read(ref cache);

            // Reference equality check for all inputs
            if (current is not null &&
                ReferenceEquals(state1, current.Input1) &&
                ReferenceEquals(state2, current.Input2) &&
                ReferenceEquals(state3, current.Input3))
            {
                return current.Result;
            }

            // At least one state changed, recompute and update cache atomically
            TResult result = selector(state1, state2, state3);
            Volatile.Write(ref cache, new(state1, state2, state3, result));
            return result;
        };
    }

    /// <summary>
    ///     Immutable cache entry holding input state and computed result together.
    ///     Enables lock-free atomic updates via single reference write.
    /// </summary>
    private sealed class CacheEntry<TState, TResult>
        where TState : class
    {
        public CacheEntry(
            TState input,
            TResult result
        )
        {
            Input = input;
            Result = result;
        }

        public TState Input { get; }

        public TResult Result { get; }
    }

    /// <summary>
    ///     Immutable cache entry for two-state selectors.
    /// </summary>
    private sealed class CacheEntry2<TState1, TState2, TResult>
        where TState1 : class
        where TState2 : class
    {
        public CacheEntry2(
            TState1 input1,
            TState2 input2,
            TResult result
        )
        {
            Input1 = input1;
            Input2 = input2;
            Result = result;
        }

        public TState1 Input1 { get; }

        public TState2 Input2 { get; }

        public TResult Result { get; }
    }

    /// <summary>
    ///     Immutable cache entry for three-state selectors.
    /// </summary>
    private sealed class CacheEntry3<TState1, TState2, TState3, TResult>
        where TState1 : class
        where TState2 : class
        where TState3 : class
    {
        public CacheEntry3(
            TState1 input1,
            TState2 input2,
            TState3 input3,
            TResult result
        )
        {
            Input1 = input1;
            Input2 = input2;
            Input3 = input3;
            Result = result;
        }

        public TState1 Input1 { get; }

        public TState2 Input2 { get; }

        public TState3 Input3 { get; }

        public TResult Result { get; }
    }
}