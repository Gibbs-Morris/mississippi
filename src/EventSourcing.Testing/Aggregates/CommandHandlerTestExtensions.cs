using System;
using System.Collections.Generic;

using FluentAssertions;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Testing.Aggregates;

/// <summary>
///     Extension methods for testing command handlers with a fluent API.
/// </summary>
/// <remarks>
///     <para>
///         These extensions provide patterns for testing command handlers in isolation
///         or as part of full aggregate scenarios.
///     </para>
///     <para>
///         <strong>Isolated Handler Testing:</strong>
///         Use <see cref="ShouldEmit{TCommand,TEvent,TAggregate}" /> for quick handler tests.
///     </para>
///     <para>
///         <strong>Full Scenario Testing:</strong>
///         Use <see cref="ForAggregate{TAggregate}" /> to create a harness with multiple handlers/reducers.
///     </para>
///     <example>
///         <code>
///         // Pattern 1: Isolated handler test
///         handler.ShouldEmit(null, command, expectedEvent);
/// 
///         // Pattern 2: Full scenario
///         CommandHandlerTestExtensions.ForAggregate&lt;BankAccountAggregate&gt;()
///             .WithHandler&lt;DepositFundsHandler&gt;()
///             .WithReducer&lt;FundsDepositedReducer&gt;()
///             .CreateScenario()
///             .Given(accountOpenedEvent)
///             .When(depositCommand)
///             .ThenEmits&lt;FundsDeposited&gt;();
///         </code>
///     </example>
/// </remarks>
public static class CommandHandlerTestExtensions
{
    /// <summary>
    ///     Creates a new test harness for the specified aggregate type.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type to test.</typeparam>
    /// <returns>A new <see cref="AggregateTestHarness{TAggregate}" />.</returns>
    public static AggregateTestHarness<TAggregate> ForAggregate<TAggregate>()
        where TAggregate : new() =>
        new();

    /// <summary>
    ///     Executes a command handler and returns the result.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="handler">The handler to test.</param>
    /// <param name="state">The current aggregate state (null uses default).</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>The operation result from the handler.</returns>
    public static OperationResult<IReadOnlyList<object>> Handle<TCommand, TAggregate>(
        this ICommandHandler<TCommand, TAggregate> handler,
        TAggregate? state,
        TCommand command
    )
        where TCommand : class
        where TAggregate : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(command);
        TAggregate actualState = state ?? new TAggregate();
        return handler.Handle(command, actualState);
    }

    /// <summary>
    ///     Executes a command handler and returns the emitted events if successful.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="handler">The handler to test.</param>
    /// <param name="state">The current aggregate state (null uses default).</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>The events emitted by the handler, or empty if failed.</returns>
    public static IReadOnlyList<object> HandleEvents<TCommand, TAggregate>(
        this ICommandHandler<TCommand, TAggregate> handler,
        TAggregate? state,
        TCommand command
    )
        where TCommand : class
        where TAggregate : new()
    {
        OperationResult<IReadOnlyList<object>> result = handler.Handle(state, command);
        return result.Success ? result.Value : Array.Empty<object>();
    }

    /// <summary>
    ///     Asserts that a handler emits a specific event.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="handler">The handler to test.</param>
    /// <param name="state">The current aggregate state (null uses default).</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="expectedEvent">The expected event to be emitted.</param>
    [CustomAssertion]
    public static void ShouldEmit<TCommand, TEvent, TAggregate>(
        this ICommandHandler<TCommand, TAggregate> handler,
        TAggregate? state,
        TCommand command,
        TEvent expectedEvent
    )
        where TCommand : class
        where TEvent : class
        where TAggregate : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(expectedEvent);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(state, command);
        result.Success.Should()
            .BeTrue("Handler should succeed, but failed with: {0} - {1}", result.ErrorCode, result.ErrorMessage);
        result.Value.Should().ContainSingle().Which.Should().BeEquivalentTo(expectedEvent);
    }

    /// <summary>
    ///     Asserts that a handler emits multiple events in order.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="handler">The handler to test.</param>
    /// <param name="state">The current aggregate state (null uses default).</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="expectedEvents">The expected events in order.</param>
    [CustomAssertion]
    public static void ShouldEmitEvents<TCommand, TAggregate>(
        this ICommandHandler<TCommand, TAggregate> handler,
        TAggregate? state,
        TCommand command,
        params object[] expectedEvents
    )
        where TCommand : class
        where TAggregate : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(expectedEvents);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(state, command);
        result.Success.Should()
            .BeTrue("Handler should succeed, but failed with: {0} - {1}", result.ErrorCode, result.ErrorMessage);
        result.Value.Should().BeEquivalentTo(expectedEvents, options => options.WithStrictOrdering());
    }

    /// <summary>
    ///     Asserts that a handler fails (returns an error result).
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="handler">The handler to test.</param>
    /// <param name="state">The current aggregate state (null uses default).</param>
    /// <param name="command">The command to execute.</param>
    [CustomAssertion]
    public static void ShouldFail<TCommand, TAggregate>(
        this ICommandHandler<TCommand, TAggregate> handler,
        TAggregate? state,
        TCommand command
    )
        where TCommand : class
        where TAggregate : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(command);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(state, command);
        result.Success.Should().BeFalse("Handler should fail but succeeded with {0} events", result.Value?.Count ?? 0);
    }

    /// <summary>
    ///     Asserts that a handler fails with a specific error code.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="handler">The handler to test.</param>
    /// <param name="state">The current aggregate state (null uses default).</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="expectedErrorCode">Expected error code string in the result.</param>
    [CustomAssertion]
    public static void ShouldFail<TCommand, TAggregate>(
        this ICommandHandler<TCommand, TAggregate> handler,
        TAggregate? state,
        TCommand command,
        string expectedErrorCode
    )
        where TCommand : class
        where TAggregate : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(expectedErrorCode);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(state, command);
        result.Success.Should().BeFalse("Handler should fail but succeeded with {0} events", result.Value?.Count ?? 0);
        result.ErrorCode.Should().Be(expectedErrorCode);
    }

    /// <summary>
    ///     Asserts that a handler fails and returns an error message containing the expected substring.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="handler">The handler to test.</param>
    /// <param name="state">The current aggregate state (null uses default).</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="expectedMessage">Expected substring in the error message.</param>
    [CustomAssertion]
    public static void ShouldFailWithMessage<TCommand, TAggregate>(
        this ICommandHandler<TCommand, TAggregate> handler,
        TAggregate? state,
        TCommand command,
        string expectedMessage
    )
        where TCommand : class
        where TAggregate : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(expectedMessage);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(state, command);
        result.Success.Should().BeFalse("Handler should fail but succeeded with {0} events", result.Value?.Count ?? 0);
        result.ErrorMessage.Should().Contain(expectedMessage);
    }

    /// <summary>
    ///     Asserts that a handler fails with a specific error code and message.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="handler">The handler to test.</param>
    /// <param name="state">The current aggregate state (null uses default).</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="expectedErrorCode">Expected error code string in the result.</param>
    /// <param name="expectedMessage">Expected substring in the error message.</param>
    [CustomAssertion]
    public static void ShouldFailWithMessage<TCommand, TAggregate>(
        this ICommandHandler<TCommand, TAggregate> handler,
        TAggregate? state,
        TCommand command,
        string expectedErrorCode,
        string expectedMessage
    )
        where TCommand : class
        where TAggregate : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(expectedErrorCode);
        ArgumentNullException.ThrowIfNull(expectedMessage);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(state, command);
        result.Success.Should().BeFalse("Handler should fail but succeeded with {0} events", result.Value?.Count ?? 0);
        result.ErrorCode.Should().Be(expectedErrorCode);
        result.ErrorMessage.Should().Contain(expectedMessage);
    }

    /// <summary>
    ///     Asserts that a handler succeeds (emits at least one event).
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="handler">The handler to test.</param>
    /// <param name="state">The current aggregate state (null uses default).</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>The emitted events for further assertions.</returns>
    [CustomAssertion]
    public static IReadOnlyList<object> ShouldSucceed<TCommand, TAggregate>(
        this ICommandHandler<TCommand, TAggregate> handler,
        TAggregate? state,
        TCommand command
    )
        where TCommand : class
        where TAggregate : new()
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(command);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(state, command);
        result.Success.Should()
            .BeTrue("Handler should succeed, but failed with: {0} - {1}", result.ErrorCode, result.ErrorMessage);
        result.Value.Should().NotBeEmpty("Handler should emit at least one event on success");
        return result.Value;
    }
}