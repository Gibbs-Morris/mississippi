using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using FluentAssertions;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Testing.Aggregates;

/// <summary>
///     A fluent scenario builder for Given/When/Then style aggregate testing.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type being tested.</typeparam>
/// <remarks>
///     <para>
///         Use this class to build readable test scenarios that establish state via events (Given),
///         execute a command (When), and verify emitted events and resulting state (Then).
///     </para>
///     <code>
///         harness.CreateScenario()
///             .Given(new AccountOpened { HolderName = "John", InitialDeposit = 100m })
///             .When(new DepositFunds { Amount = 50m })
///             .ThenEmits&lt;FundsDeposited&gt;(e =&gt; e.Amount.Should().Be(50m))
///             .ThenState(s =&gt; s.Balance.Should().Be(150m));
///     </code>
/// </remarks>
public sealed class AggregateScenario<TAggregate>
    where TAggregate : new()
{
    private readonly List<object> emittedEvents = [];

    private readonly List<object> givenEvents = [];

    private readonly IReadOnlyList<ICommandHandler<TAggregate>> handlers;

    private readonly IReadOnlyList<IEventReducer<TAggregate>> reducers;

    private bool lastCommandSucceeded = true;

    private string? lastErrorCode;

    private string? lastErrorMessage;

    private object? whenCommand;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateScenario{TAggregate}" /> class.
    /// </summary>
    /// <param name="handlers">The command handlers to use.</param>
    /// <param name="reducers">The reducers to use for applying events.</param>
    /// <param name="initialState">The initial aggregate state.</param>
    internal AggregateScenario(
        IReadOnlyList<ICommandHandler<TAggregate>> handlers,
        IReadOnlyList<IEventReducer<TAggregate>> reducers,
        TAggregate initialState
    )
    {
        this.handlers = handlers;
        this.reducers = reducers;
        State = initialState;
    }

    /// <summary>
    ///     Gets all events that have been applied (Given events + emitted events).
    /// </summary>
    public IReadOnlyList<object> AllAppliedEvents => givenEvents.Concat(emittedEvents).ToList();

    /// <summary>
    ///     Gets the events emitted by the When command.
    /// </summary>
    public IReadOnlyList<object> EmittedEvents => emittedEvents;

    /// <summary>
    ///     Gets the current aggregate state after applying all events.
    /// </summary>
    public TAggregate State { get; private set; }

    private static TAggregate ApplyWithReducer(
        IEventReducer<TAggregate> reducer,
        TAggregate state,
        object evt
    )
    {
        Type reducerType = reducer.GetType();
        Type eventType = evt.GetType();
        Type? iface = reducerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 (i.GetGenericTypeDefinition() == typeof(IEventReducer<,>)) &&
                                 (i.GetGenericArguments()[0] == eventType));
        if (iface is not null)
        {
            MethodInfo? method = iface.GetMethod("Reduce");
            return (TAggregate)method!.Invoke(reducer, [state, evt])!;
        }

        throw new InvalidOperationException($"Cannot apply event {eventType.Name}.");
    }

    private static bool CanHandle(
        IEventReducer<TAggregate> reducer,
        object evt
    )
    {
        Type reducerType = reducer.GetType();
        Type eventType = evt.GetType();
        return reducerType.GetInterfaces()
            .Any(iface => iface.IsGenericType &&
                          (iface.GetGenericTypeDefinition() == typeof(IEventReducer<,>)) &&
                          (iface.GetGenericArguments()[0] == eventType));
    }

    private static bool CanHandleCommand(
        ICommandHandler<TAggregate> handler,
        object command
    )
    {
        Type handlerType = handler.GetType();
        Type commandType = command.GetType();
        return handlerType.GetInterfaces()
            .Any(iface => iface.IsGenericType &&
                          (iface.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)) &&
                          (iface.GetGenericArguments()[0] == commandType));
    }

    private static (IEnumerable<object> Events, bool Success, string? ErrorCode, string? ErrorMessage)
        ExecuteWithHandler(
            ICommandHandler<TAggregate> handler,
            TAggregate state,
            object command
        )
    {
        Type handlerType = handler.GetType();
        Type commandType = command.GetType();
        Type? iface = handlerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)) &&
                                 (i.GetGenericArguments()[0] == commandType));
        if (iface is not null)
        {
            MethodInfo? method = iface.GetMethod("Handle");
            object result = method!.Invoke(handler, [command, state])!;

            // Handle OperationResult<IReadOnlyList<object>> return type
            Type resultType = result.GetType();
            PropertyInfo? successProp = resultType.GetProperty("Success");
            PropertyInfo? valueProp = resultType.GetProperty("Value");
            PropertyInfo? errorCodeProp = resultType.GetProperty("ErrorCode");
            PropertyInfo? errorMessageProp = resultType.GetProperty("ErrorMessage");
            bool success = (bool)successProp!.GetValue(result)!;
            if (success)
            {
                IEnumerable<object> events = (IEnumerable<object>)valueProp!.GetValue(result)!;
                return (events, true, null, null);
            }

            // On failure, return empty collection with error info
            string? errorCode = errorCodeProp?.GetValue(result)?.ToString();
            string? errorMessage = errorMessageProp?.GetValue(result)?.ToString();
            return (Array.Empty<object>(), false, errorCode, errorMessage);
        }

        throw new InvalidOperationException($"Cannot execute command {commandType.Name}.");
    }

    /// <summary>
    ///     Adds a historical event to establish the starting state.
    /// </summary>
    /// <param name="eventData">The event to apply to establish state.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    public AggregateScenario<TAggregate> Given(
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        givenEvents.Add(eventData);
        State = ApplyEvent(State, eventData);
        return this;
    }

    /// <summary>
    ///     Adds multiple historical events to establish the starting state.
    /// </summary>
    /// <param name="events">The events to apply to establish state.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    public AggregateScenario<TAggregate> Given(
        params object[] events
    )
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (object evt in events)
        {
            Given(evt);
        }

        return this;
    }

    /// <summary>
    ///     Asserts that a specific event type was emitted.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="assertion">Optional assertion to run against the event.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    public AggregateScenario<TAggregate> ThenEmits<TEvent>(
        Action<TEvent>? assertion = null
    )
        where TEvent : class
    {
        whenCommand.Should().NotBeNull("When() must be called before ThenEmits()");
        TEvent? evt = emittedEvents.OfType<TEvent>().FirstOrDefault();
        evt.Should().NotBeNull($"Expected event of type {typeof(TEvent).Name} to be emitted");
        assertion?.Invoke(evt!);
        return this;
    }

    /// <summary>
    ///     Asserts that multiple events were emitted in order.
    /// </summary>
    /// <param name="assertions">Assertions to run against each emitted event in order.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    public AggregateScenario<TAggregate> ThenEmitsEvents(
        params Action<object>[] assertions
    )
    {
        ArgumentNullException.ThrowIfNull(assertions);
        whenCommand.Should().NotBeNull("When() must be called before ThenEmitsEvents()");
        emittedEvents.Should().HaveCount(assertions.Length);
        for (int i = 0; i < assertions.Length; i++)
        {
            assertions[i](emittedEvents[i]);
        }

        return this;
    }

    /// <summary>
    ///     Asserts that the command failed with a specific error code.
    /// </summary>
    /// <param name="expectedErrorCode">The expected error code.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    [CustomAssertion]
    public AggregateScenario<TAggregate> ThenFails(
        string expectedErrorCode
    )
    {
        ArgumentNullException.ThrowIfNull(expectedErrorCode);
        whenCommand.Should().NotBeNull("When() must be called before ThenFails()");
        lastCommandSucceeded.Should()
            .BeFalse("Command should have failed but succeeded with {0} events", emittedEvents.Count);
        lastErrorCode.Should().NotBeNull("Expected an error code from the failed command");
        lastErrorCode.Should().Be(expectedErrorCode);
        return this;
    }

    /// <summary>
    ///     Asserts that the command failed with a specific error code and message.
    /// </summary>
    /// <param name="expectedErrorCode">The expected error code.</param>
    /// <param name="expectedMessage">Expected substring in the failure message.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    [CustomAssertion]
    public AggregateScenario<TAggregate> ThenFails(
        string expectedErrorCode,
        string expectedMessage
    )
    {
        ArgumentNullException.ThrowIfNull(expectedErrorCode);
        ArgumentNullException.ThrowIfNull(expectedMessage);
        whenCommand.Should().NotBeNull("When() must be called before ThenFails()");
        lastCommandSucceeded.Should()
            .BeFalse("Command should have failed but succeeded with {0} events", emittedEvents.Count);
        lastErrorCode.Should().NotBeNull("Expected an error code from the failed command");
        lastErrorCode.Should().Be(expectedErrorCode);
        lastErrorMessage.Should().NotBeNull("Expected an error message from the failed command");
        lastErrorMessage.Should().Contain(expectedMessage);
        return this;
    }

    /// <summary>
    ///     Asserts against the resulting aggregate state.
    /// </summary>
    /// <param name="assertion">The assertion to execute against the state.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    public AggregateScenario<TAggregate> ThenState(
        Action<TAggregate> assertion
    )
    {
        ArgumentNullException.ThrowIfNull(assertion);
        whenCommand.Should().NotBeNull("When() must be called before ThenState()");
        assertion(State);
        return this;
    }

    /// <summary>
    ///     Asserts that the command succeeded (emitted at least one non-failure event).
    /// </summary>
    /// <returns>This scenario for fluent chaining.</returns>
    [CustomAssertion]
    public AggregateScenario<TAggregate> ThenSucceeds()
    {
        whenCommand.Should().NotBeNull("When() must be called before ThenSucceeds()");
        emittedEvents.Should().NotBeEmpty("Command should emit at least one event on success");

        // Check that no failure events were emitted
        emittedEvents.Select(evt => evt.GetType().Name)
            .Should()
            .NotContain(
                name => name.Contains("Failed", StringComparison.Ordinal),
                "Expected success events, but got a failure event");

        return this;
    }

    /// <summary>
    ///     Executes a command against the current state.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no handler is found for the command.</exception>
    public AggregateScenario<TAggregate> When(object command)
    {
        ArgumentNullException.ThrowIfNull(command);
        whenCommand = command;

        // Find handler and execute
        (IEnumerable<object> events, bool success, string? errorCode, string? errorMessage) =
            ExecuteCommand(State, command);
        lastCommandSucceeded = success;
        lastErrorCode = errorCode;
        lastErrorMessage = errorMessage;
        foreach (object evt in events)
        {
            emittedEvents.Add(evt);
            State = ApplyEvent(State, evt);
        }

        return this;
    }

    private TAggregate ApplyEvent(
        TAggregate state,
        object evt
    )
    {
        IEventReducer<TAggregate>? reducer = reducers.FirstOrDefault(r => CanHandle(r, evt));
        if (reducer is not null)
        {
            return ApplyWithReducer(reducer, state, evt);
        }

        throw new InvalidOperationException($"No reducer registered for event type {evt.GetType().Name}.");
    }

    private (IEnumerable<object> Events, bool Success, string? ErrorCode, string? ErrorMessage) ExecuteCommand(
        TAggregate state,
        object command
    )
    {
        ICommandHandler<TAggregate>? handler = handlers.FirstOrDefault(h => CanHandleCommand(h, command));
        if (handler is not null)
        {
            return ExecuteWithHandler(handler, state, command);
        }

        throw new InvalidOperationException($"No handler registered for command type {command.GetType().Name}.");
    }
}