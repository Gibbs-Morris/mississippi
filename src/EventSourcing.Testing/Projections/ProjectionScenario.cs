using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;
using FluentAssertions.Extensibility;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Testing.Projections;

/// <summary>
///     A fluent scenario builder for Given/When/Then style projection testing.
/// </summary>
/// <typeparam name="TProjection">The projection type being tested.</typeparam>
/// <remarks>
///     <para>
///         Use this class to build readable test scenarios that establish state via events (Given),
///         apply a new event (When), and verify the resulting projection (Then).
///     </para>
///     <code>
///         harness.CreateScenario()
///             .Given(new AccountOpened { HolderName = "John", InitialDeposit = 100m })
///             .When(new FundsDeposited { Amount = 50m })
///             .ThenAssert(p =&gt; p.Balance.Should().Be(150m));
///     </code>
/// </remarks>
public sealed class ProjectionScenario<TProjection>
    where TProjection : new()
{
    private readonly List<object> appliedEvents = [];
    private readonly List<object> givenEvents = [];
    private readonly IReadOnlyList<IEventReducer<TProjection>> reducers;
    private readonly List<object> whenEvents = [];
    private TProjection currentState;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionScenario{TProjection}" /> class.
    /// </summary>
    /// <param name="reducers">The reducers to use for applying events.</param>
    /// <param name="initialState">The initial projection state.</param>
    internal ProjectionScenario(IReadOnlyList<IEventReducer<TProjection>> reducers, TProjection initialState)
    {
        this.reducers = reducers;
        currentState = initialState;
    }

    /// <summary>
    ///     Gets the current projection state after applying all events.
    /// </summary>
    public TProjection State => currentState;

    /// <summary>
    ///     Gets all events that have been applied.
    /// </summary>
    public IReadOnlyList<object> AppliedEvents => appliedEvents;

    /// <summary>
    ///     Adds a historical event to establish the starting state.
    /// </summary>
    /// <param name="eventData">The event to apply to establish state.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    public ProjectionScenario<TProjection> Given(object eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        givenEvents.Add(eventData);
        appliedEvents.Add(eventData);
        currentState = ApplyEvent(currentState, eventData);
        return this;
    }

    /// <summary>
    ///     Adds multiple historical events to establish the starting state.
    /// </summary>
    /// <param name="events">The events to apply to establish state.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    public ProjectionScenario<TProjection> Given(params object[] events)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (object evt in events)
        {
            Given(evt);
        }

        return this;
    }

    /// <summary>
    ///     Sets the event being tested (the action under test).
    /// </summary>
    /// <param name="eventData">The event to apply and test.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    public ProjectionScenario<TProjection> When(object eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        whenEvents.Add(eventData);
        appliedEvents.Add(eventData);
        currentState = ApplyEvent(currentState, eventData);
        return this;
    }

    /// <summary>
    ///     Executes an assertion against the resulting projection state.
    /// </summary>
    /// <param name="assertion">The assertion to execute.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    [CustomAssertion]
    public ProjectionScenario<TProjection> ThenAssert(Action<TProjection> assertion)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        assertion(currentState);
        return this;
    }

    /// <summary>
    ///     Executes an assertion against the resulting projection state. Alias for <see cref="ThenAssert" />.
    /// </summary>
    /// <param name="assertion">The assertion to execute.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    [CustomAssertion]
    public ProjectionScenario<TProjection> ThenShouldSatisfy(Action<TProjection> assertion) => ThenAssert(assertion);

    /// <summary>
    ///     Verifies the resulting projection satisfies a predicate condition.
    /// </summary>
    /// <param name="predicate">The predicate that must return true.</param>
    /// <param name="because">The reason for the assertion.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    [CustomAssertion]
    public ProjectionScenario<TProjection> ThenShouldSatisfy(Func<TProjection, bool> predicate, string because)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        predicate(currentState).Should().BeTrue(because);
        return this;
    }

    /// <summary>
    ///     Verifies the resulting projection matches the expected state.
    /// </summary>
    /// <param name="expected">The expected projection state.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    [CustomAssertion]
    public ProjectionScenario<TProjection> ThenEquals(TProjection expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        currentState.Should().BeEquivalentTo(expected);
        return this;
    }

    /// <summary>
    ///     Verifies the resulting projection matches the expected state. Alias for <see cref="ThenEquals" />.
    /// </summary>
    /// <param name="expected">The expected projection state.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    [CustomAssertion]
    public ProjectionScenario<TProjection> ThenShouldBe(TProjection expected) => ThenEquals(expected);

    private TProjection ApplyEvent(TProjection state, object evt)
    {
        foreach (IEventReducer<TProjection> reducer in reducers)
        {
            if (CanHandle(reducer, evt))
            {
                return ApplyWithReducer(reducer, state, evt);
            }
        }

        throw new InvalidOperationException(
            $"No reducer registered for event type {evt.GetType().Name}.");
    }

    private static bool CanHandle(IEventReducer<TProjection> reducer, object evt)
    {
        Type reducerType = reducer.GetType();
        Type eventType = evt.GetType();

        foreach (Type iface in reducerType.GetInterfaces())
        {
            if (iface.IsGenericType &&
                iface.GetGenericTypeDefinition() == typeof(IEventReducer<,>) &&
                iface.GetGenericArguments()[0] == eventType)
            {
                return true;
            }
        }

        return false;
    }

    private static TProjection ApplyWithReducer(IEventReducer<TProjection> reducer, TProjection state, object evt)
    {
        Type reducerType = reducer.GetType();
        Type eventType = evt.GetType();

        foreach (Type iface in reducerType.GetInterfaces())
        {
            if (iface.IsGenericType &&
                iface.GetGenericTypeDefinition() == typeof(IEventReducer<,>) &&
                iface.GetGenericArguments()[0] == eventType)
            {
                System.Reflection.MethodInfo? method = iface.GetMethod("Reduce");
                return (TProjection)method!.Invoke(reducer, [state, evt])!;
            }
        }

        throw new InvalidOperationException($"Cannot apply event {eventType.Name}.");
    }
}
