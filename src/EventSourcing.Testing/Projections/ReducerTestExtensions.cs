using System;

using FluentAssertions;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Testing.Projections;

/// <summary>
///     Extension methods for testing event reducers with a fluent API.
/// </summary>
/// <remarks>
///     <para>
///         These extensions provide two testing patterns:
///     </para>
///     <list type="number">
///         <item>
///             <term>Direct Apply</term>
///             <description>
///                 Use <see cref="Apply{TEvent,TProjection}" /> for quick reducer invocation
///                 when you want to make custom assertions on the result.
///             </description>
///         </item>
///         <item>
///             <term>ShouldProduce</term>
///             <description>
///                 Use <see cref="ShouldProduce{TEvent,TProjection}" /> when you have an
///                 expected output and want to verify the reducer produces it exactly.
///             </description>
///         </item>
///     </list>
/// </remarks>
public static class ReducerTestExtensions
{
    /// <summary>
    ///     Applies an event to a reducer and returns the resulting projection.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="reducer">The reducer to test.</param>
    /// <param name="initialState">The initial projection state (null uses default).</param>
    /// <param name="eventData">The event to apply.</param>
    /// <returns>The resulting projection after applying the event.</returns>
    /// <exception cref="ArgumentNullException">Thrown if reducer or eventData is null.</exception>
    public static TProjection Apply<TEvent, TProjection>(
        this IEventReducer<TEvent, TProjection> reducer,
        TProjection? initialState,
        TEvent eventData
    )
        where TEvent : class
        where TProjection : new()
    {
        ArgumentNullException.ThrowIfNull(reducer);
        ArgumentNullException.ThrowIfNull(eventData);
        TProjection state = initialState ?? new TProjection();
        return reducer.Reduce(state, eventData);
    }

    /// <summary>
    ///     Creates a new test harness for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">The projection type to test.</typeparam>
    /// <returns>A new <see cref="ReducerTestHarness{TProjection}" />.</returns>
    public static ReducerTestHarness<TProjection> ForProjection<TProjection>()
        where TProjection : new() =>
        new();

    /// <summary>
    ///     Asserts that a reducer produces the expected projection when given an event.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="reducer">The reducer to test.</param>
    /// <param name="initialState">The initial projection state (null uses default).</param>
    /// <param name="eventData">The event to apply.</param>
    /// <param name="expected">The expected resulting projection.</param>
    /// <exception cref="ArgumentNullException">Thrown if reducer, eventData, or expected is null.</exception>
    [CustomAssertion]
    public static void ShouldProduce<TEvent, TProjection>(
        this IEventReducer<TEvent, TProjection> reducer,
        TProjection? initialState,
        TEvent eventData,
        TProjection expected
    )
        where TEvent : class
        where TProjection : new()
    {
        ArgumentNullException.ThrowIfNull(reducer);
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(expected);
        TProjection result = reducer.Apply(initialState, eventData);
        result.Should().BeEquivalentTo(expected);
    }

    /// <summary>
    ///     Asserts that a reducer throws an exception of the specified type.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="reducer">The reducer to test.</param>
    /// <param name="initialState">The initial projection state (null uses default).</param>
    /// <param name="eventData">The event to apply.</param>
    /// <param name="expectedMessage">Optional: expected exception message substring.</param>
    /// <exception cref="ArgumentNullException">Thrown if reducer or eventData is null.</exception>
    [CustomAssertion]
    public static void ShouldThrow<TException, TEvent, TProjection>(
        this IEventReducer<TEvent, TProjection> reducer,
        TProjection? initialState,
        TEvent? eventData,
        string? expectedMessage = null
    )
        where TException : Exception
        where TEvent : class
        where TProjection : new()
    {
        ArgumentNullException.ThrowIfNull(reducer);
        TProjection state = initialState ?? new TProjection();
        Action act = () => reducer.Reduce(state, eventData!);
        if (expectedMessage is not null)
        {
            act.Should().Throw<TException>().WithMessage($"*{expectedMessage}*");
        }
        else
        {
            act.Should().Throw<TException>();
        }
    }
}