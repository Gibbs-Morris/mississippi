using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Testing;

/// <summary>
///     A fluent scenario builder for Given/When/Then style state testing.
/// </summary>
/// <typeparam name="TState">The feature state type being tested.</typeparam>
public sealed class StoreScenario<TState>
    where TState : class, IFeatureState, new()
{
    private readonly List<IAction> dispatchedActions = [];

    private readonly List<IActionEffect<TState>> effects;

    private readonly List<IAction> emittedActions = [];

    private readonly List<IAction> givenActions = [];

    private readonly List<Func<TState, IAction, TState>> reducers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StoreScenario{TState}" /> class.
    /// </summary>
    /// <param name="reducers">The reducers to use.</param>
    /// <param name="effects">The effects to use.</param>
    /// <param name="initialState">The initial state.</param>
    /// <param name="serviceProvider">The service provider for effect dependencies.</param>
    internal StoreScenario(
        List<Func<TState, IAction, TState>> reducers,
        List<IActionEffect<TState>> effects,
        TState initialState,
        IServiceProvider serviceProvider
    )
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        this.reducers = reducers;
        this.effects = effects;
        State = initialState;
    }

    /// <summary>
    ///     Gets all dispatched actions in order.
    /// </summary>
    public IReadOnlyList<IAction> DispatchedActions => dispatchedActions;

    /// <summary>
    ///     Gets actions emitted by effects.
    /// </summary>
    public IReadOnlyList<IAction> EmittedActions => emittedActions;

    /// <summary>
    ///     Gets the current state.
    /// </summary>
    public TState State { get; private set; }

    /// <summary>
    ///     Establishes initial state by applying actions through reducers.
    /// </summary>
    /// <param name="actions">The actions to apply.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="actions" /> is null.</exception>
    public StoreScenario<TState> Given(
        params IAction[] actions
    )
    {
        ArgumentNullException.ThrowIfNull(actions);
        foreach (IAction action in actions)
        {
            givenActions.Add(action);
            State = ApplyReducers(action);
        }

        return this;
    }

    /// <summary>
    ///     Sets the state directly without applying actions.
    /// </summary>
    /// <param name="state">The state to set.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="state" /> is null.</exception>
    public StoreScenario<TState> GivenState(
        TState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        State = state;
        return this;
    }

    /// <summary>
    ///     Asserts that a specific action type was emitted by effects.
    /// </summary>
    /// <typeparam name="TAction">The expected action type.</typeparam>
    /// <param name="assertion">Optional assertion on the action.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    public StoreScenario<TState> ThenEmits<TAction>(
        Action<TAction>? assertion = null
    )
        where TAction : IAction
    {
        TAction? emitted = emittedActions.OfType<TAction>().FirstOrDefault();
        if (emitted is null)
        {
            throw new InvalidOperationException(
                $"Expected action of type {typeof(TAction).Name} to be emitted, but it was not. " +
                $"Emitted actions: [{string.Join(", ", emittedActions.Select(a => a.GetType().Name))}]");
        }

        assertion?.Invoke(emitted);
        return this;
    }

    /// <summary>
    ///     Asserts that no actions were emitted by effects.
    /// </summary>
    /// <returns>This scenario for fluent chaining.</returns>
    public StoreScenario<TState> ThenEmitsNothing()
    {
        if (emittedActions.Count > 0)
        {
            throw new InvalidOperationException(
                $"Expected no actions to be emitted, but found: [{string.Join(", ", emittedActions.Select(a => a.GetType().Name))}]");
        }

        return this;
    }

    /// <summary>
    ///     Asserts on the resulting state.
    /// </summary>
    /// <param name="assertion">The assertion to run on the state.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="assertion" /> is null.</exception>
    public StoreScenario<TState> ThenState(
        Action<TState> assertion
    )
    {
        ArgumentNullException.ThrowIfNull(assertion);
        assertion(State);
        return this;
    }

    /// <summary>
    ///     Dispatches an action through reducers and effects.
    /// </summary>
    /// <param name="action">The action to dispatch.</param>
    /// <returns>This scenario for fluent chaining.</returns>
    /// <remarks>
    ///     This method blocks on async effects. For async scenarios, use <see cref="WhenAsync" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="action" /> is null.</exception>
#pragma warning disable VSTHRD002 // Avoid synchronously waiting on tasks in test harness fluent API
    public StoreScenario<TState> When(
        IAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        dispatchedActions.Add(action);

        // Apply reducers
        State = ApplyReducers(action);

        // Run effects and collect emitted actions
        RunEffectsAsync(action).GetAwaiter().GetResult();
        return this;
    }
#pragma warning restore VSTHRD002

    /// <summary>
    ///     Dispatches an action through reducers and effects asynchronously.
    /// </summary>
    /// <param name="action">The action to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="action" /> is null.</exception>
    public async Task<StoreScenario<TState>> WhenAsync(
        IAction action,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        dispatchedActions.Add(action);

        // Apply reducers
        State = ApplyReducers(action);

        // Run effects and collect emitted actions
        await RunEffectsAsync(action, cancellationToken).ConfigureAwait(false);
        return this;
    }

    private TState ApplyReducers(
        IAction action
    )
    {
        TState currentState = State;
        foreach (Func<TState, IAction, TState> reducer in reducers)
        {
            currentState = reducer(currentState, action);
        }

        return currentState;
    }

    private async Task RunEffectsAsync(
        IAction action,
        CancellationToken cancellationToken = default
    )
    {
        foreach (IActionEffect<TState> effect in effects)
        {
            if (!effect.CanHandle(action))
            {
                continue;
            }

            await foreach (IAction emitted in effect.HandleAsync(action, State, cancellationToken)
                               .ConfigureAwait(false))
            {
                emittedActions.Add(emitted);
            }
        }
    }
}