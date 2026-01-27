using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Testing.Aggregates;

/// <summary>
///     A fluent test harness for testing aggregate command handlers and reducers together.
/// </summary>
/// <remarks>
///     <para>
///         This harness enables full aggregate testing by combining command handlers
///         (which produce events) with reducers (which apply events to state).
///         It supports Given/When/Then style scenarios for complete aggregate workflows.
///     </para>
///     <para>
///         <strong>Pattern Overview:</strong>
///     </para>
///     <list type="number">
///         <item>
///             <term>Given (events)</term>
///             <description>Establish starting state by replaying historical events through reducers.</description>
///         </item>
///         <item>
///             <term>When (command)</term>
///             <description>Execute a command against current state via handlers, producing new events.</description>
///         </item>
///         <item>
///             <term>Then (assertions)</term>
///             <description>Assert on emitted events and/or resulting state after applying events.</description>
///         </item>
///     </list>
///     <para>
///         <strong>Example:</strong>
///     </para>
///     <code>
///         CommandHandlerTestExtensions.ForAggregate&lt;BankAccountAggregate&gt;()
///             .WithHandler&lt;OpenAccountHandler&gt;()
///             .WithHandler&lt;DepositFundsHandler&gt;()
///             .WithReducer&lt;AccountOpenedReducer&gt;()
///             .WithReducer&lt;FundsDepositedReducer&gt;()
///             .CreateScenario()
///             .Given(new AccountOpened { HolderName = "Test", InitialDeposit = 100m })
///             .When(new DepositFunds { Amount = 50m })
///             .ThenEmits&lt;FundsDeposited&gt;(e =&gt; e.Amount.Should().Be(50m))
///             .ThenState(s =&gt; s.Balance.Should().Be(150m));
///     </code>
///     <para>
///         <strong>Unified Testing Approach:</strong>
///         This harness shares design patterns with
///         <see cref="Mississippi.EventSourcing.Testing.Projections.ReducerTestHarness{TProjection}" />,
///         enabling consistent testing for both projections and aggregates.
///     </para>
/// </remarks>
/// <typeparam name="TAggregate">The aggregate type being tested. Must have a parameterless constructor.</typeparam>
/// <seealso cref="AggregateScenario{TAggregate}" />
/// <seealso cref="CommandHandlerTestExtensions" />
public sealed class AggregateTestHarness<TAggregate>
    where TAggregate : new()
{
    private readonly List<ICommandHandler<TAggregate>> handlers = [];
    private readonly List<IEventReducer<TAggregate>> reducers = [];
    private TAggregate initialState;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateTestHarness{TAggregate}" /> class
    ///     with a default-constructed initial state.
    /// </summary>
    public AggregateTestHarness()
    {
        initialState = new TAggregate();
    }

    /// <summary>
    ///     Gets the handlers registered with this harness.
    /// </summary>
    internal IReadOnlyList<ICommandHandler<TAggregate>> Handlers => handlers;

    /// <summary>
    ///     Gets the reducers registered with this harness.
    /// </summary>
    internal IReadOnlyList<IEventReducer<TAggregate>> Reducers => reducers;

    /// <summary>
    ///     Gets the initial state for scenarios.
    /// </summary>
    internal TAggregate InitialState => initialState;

    /// <summary>
    ///     Registers a command handler by type.
    /// </summary>
    /// <typeparam name="THandler">
    ///     The handler type implementing <see cref="ICommandHandler{TAggregate}" />.
    /// </typeparam>
    /// <returns>This harness for fluent chaining.</returns>
    public AggregateTestHarness<TAggregate> WithHandler<THandler>()
        where THandler : ICommandHandler<TAggregate>, new()
    {
        handlers.Add(new THandler());
        return this;
    }

    /// <summary>
    ///     Registers a command handler instance directly.
    /// </summary>
    /// <param name="handler">The handler instance to register.</param>
    /// <returns>This harness for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="handler" /> is null.</exception>
    public AggregateTestHarness<TAggregate> WithHandler(ICommandHandler<TAggregate> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        handlers.Add(handler);
        return this;
    }

    /// <summary>
    ///     Registers an event reducer by type.
    /// </summary>
    /// <typeparam name="TReducer">
    ///     The reducer type implementing <see cref="IEventReducer{TAggregate}" />.
    /// </typeparam>
    /// <returns>This harness for fluent chaining.</returns>
    public AggregateTestHarness<TAggregate> WithReducer<TReducer>()
        where TReducer : IEventReducer<TAggregate>, new()
    {
        reducers.Add(new TReducer());
        return this;
    }

    /// <summary>
    ///     Registers an event reducer instance directly.
    /// </summary>
    /// <param name="reducer">The reducer instance to register.</param>
    /// <returns>This harness for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="reducer" /> is null.</exception>
    public AggregateTestHarness<TAggregate> WithReducer(IEventReducer<TAggregate> reducer)
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
    public AggregateTestHarness<TAggregate> WithInitialState(TAggregate state)
    {
        ArgumentNullException.ThrowIfNull(state);
        initialState = state;
        return this;
    }

    /// <summary>
    ///     Creates a new scenario builder for Given/When/Then style testing.
    /// </summary>
    /// <returns>A new <see cref="AggregateScenario{TAggregate}" /> initialized with this harness's handlers and reducers.</returns>
    public AggregateScenario<TAggregate> CreateScenario() =>
        new(handlers, reducers, initialState);
}
