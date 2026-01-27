using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Testing.Projections;

/// <summary>
///     A fluent test harness for testing event reducers and projections.
/// </summary>
/// <remarks>
///     <para>
///         This harness enables both single-reducer unit testing and multi-reducer scenario testing
///         using a fluent Given/When/Then style API. It is designed for L0 (pure unit) tests
///         that remain in-memory without external dependencies.
///     </para>
///     <para>
///         <strong>Single Reducer Testing (L0):</strong>
///         Use the extension methods directly on reducers for isolated unit tests.
///     </para>
///     <code>
///         // Quick apply and assert
///         var result = reducer.Apply(initialState, eventData);
///         result.Balance.Should().Be(expected);
///
///         // Or use ShouldProduce for expected output assertions
///         reducer.ShouldProduce(initialState, eventData, expectedProjection);
///     </code>
///     <para>
///         <strong>Multi-Reducer Scenario Testing (L0):</strong>
///         Use the harness to compose multiple reducers and test event replay scenarios.
///     </para>
///     <code>
///         // Create a harness with all reducers for a projection
///         var harness = ReducerTestExtensions.ForProjection&lt;BankAccountBalanceProjection&gt;()
///             .WithReducer&lt;AccountOpenedBalanceReducer&gt;()
///             .WithReducer&lt;FundsDepositedBalanceReducer&gt;()
///             .WithReducer&lt;FundsWithdrawnBalanceReducer&gt;();
///
///         // Run a scenario with Given/When/Then
///         harness.CreateScenario()
///             .Given(new AccountOpened { HolderName = "John", InitialDeposit = 100m })
///             .When(new FundsDeposited { Amount = 50m })
///             .ThenAssert(p =&gt; p.Balance.Should().Be(150m));
///     </code>
/// </remarks>
/// <typeparam name="TProjection">The projection type being tested. Must have a parameterless constructor.</typeparam>
/// <seealso cref="ProjectionScenario{TProjection}" />
/// <seealso cref="ReducerTestExtensions" />
public sealed class ReducerTestHarness<TProjection>
    where TProjection : new()
{
    private readonly List<IEventReducer<TProjection>> reducers = [];
    private TProjection initialState;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReducerTestHarness{TProjection}" /> class
    ///     with a default-constructed initial state.
    /// </summary>
    public ReducerTestHarness()
    {
        initialState = new TProjection();
    }

    /// <summary>
    ///     Gets the reducers registered with this harness.
    /// </summary>
    internal IReadOnlyList<IEventReducer<TProjection>> Reducers => reducers;

    /// <summary>
    ///     Gets the initial state for scenarios.
    /// </summary>
    internal TProjection InitialState => initialState;

    /// <summary>
    ///     Registers a reducer by type. The reducer is instantiated using its parameterless constructor.
    /// </summary>
    /// <typeparam name="TReducer">
    ///     The reducer type implementing <see cref="IEventReducer{TProjection}" />.
    /// </typeparam>
    /// <returns>This harness for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the reducer cannot be instantiated.
    /// </exception>
    public ReducerTestHarness<TProjection> WithReducer<TReducer>()
        where TReducer : IEventReducer<TProjection>, new()
    {
        reducers.Add(new TReducer());
        return this;
    }

    /// <summary>
    ///     Registers a reducer instance directly.
    /// </summary>
    /// <param name="reducer">The reducer instance to register.</param>
    /// <returns>This harness for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="reducer" /> is null.</exception>
    public ReducerTestHarness<TProjection> WithReducer(IEventReducer<TProjection> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);
        reducers.Add(reducer);
        return this;
    }

    /// <summary>
    ///     Sets a custom initial state for scenarios.
    /// </summary>
    /// <param name="state">The initial state to use.</param>
    /// <returns>This harness for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="state" /> is null.</exception>
    public ReducerTestHarness<TProjection> WithInitialState(TProjection state)
    {
        ArgumentNullException.ThrowIfNull(state);
        initialState = state;
        return this;
    }

    /// <summary>
    ///     Creates a new scenario builder for Given/When/Then style testing.
    /// </summary>
    /// <returns>A new <see cref="ProjectionScenario{TProjection}" /> initialized with this harness's reducers.</returns>
    public ProjectionScenario<TProjection> CreateScenario() =>
        new(reducers, initialState);

    /// <summary>
    ///     Applies a single event using the first matching reducer.
    /// </summary>
    /// <typeparam name="TEvent">The event type to apply.</typeparam>
    /// <param name="eventData">The event data to apply.</param>
    /// <returns>The resulting projection state after applying the event.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if no reducer handles the specified event type.
    /// </exception>
    public TProjection ApplyEvent<TEvent>(TEvent eventData)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventData);

        foreach (IEventReducer<TProjection> reducer in reducers)
        {
            if (reducer is IEventReducer<TEvent, TProjection> typedReducer)
            {
                return typedReducer.Reduce(initialState, eventData);
            }
        }

        throw new InvalidOperationException(
            $"No reducer registered for event type {typeof(TEvent).Name}. " +
            $"Register a reducer with WithReducer<TReducer>() where TReducer implements IEventReducer<{typeof(TEvent).Name}, {typeof(TProjection).Name}>.");
    }

    /// <summary>
    ///     Applies multiple events in sequence using matching reducers.
    /// </summary>
    /// <param name="events">The events to apply in order.</param>
    /// <returns>The resulting projection state after applying all events.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if no reducer handles one of the event types.
    /// </exception>
    public TProjection ApplyEvents(params object[] events)
    {
        ArgumentNullException.ThrowIfNull(events);

        TProjection state = initialState;

        foreach (object evt in events)
        {
            ArgumentNullException.ThrowIfNull(evt);

            bool handled = false;
            foreach (IEventReducer<TProjection> reducer in reducers)
            {
                if (CanHandle(reducer, evt))
                {
                    state = ApplyWithReducer(reducer, state, evt);
                    handled = true;
                    break;
                }
            }

            if (!handled)
            {
                throw new InvalidOperationException(
                    $"No reducer registered for event type {evt.GetType().Name}. " +
                    $"Register a reducer with WithReducer<TReducer>().");
            }
        }

        return state;
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

        throw new InvalidOperationException($"Cannot apply event {eventType.Name} with reducer {reducerType.Name}.");
    }
}
