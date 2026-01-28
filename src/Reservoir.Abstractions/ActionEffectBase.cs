using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Base class for action effects that handle a specific action type.
/// </summary>
/// <typeparam name="TAction">The action type this effect handles.</typeparam>
/// <typeparam name="TState">The feature state type this effect is registered for.</typeparam>
/// <remarks>
///     <para>
///         Inherit from this class to create strongly-typed action effects.
///         The base class handles type checking in <see cref="CanHandle" /> and
///         dispatches to the typed
///         <see cref="HandleAsync(TAction, TState, CancellationToken)" /> method.
///     </para>
///     <para>
///         Effects should be stateless and registered as transient services.
///     </para>
/// </remarks>
public abstract class ActionEffectBase<TAction, TState> : IActionEffect<TState>
    where TAction : IAction
    where TState : class, IFeatureState
{
    /// <inheritdoc />
    public bool CanHandle(
        IAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        return action is TAction;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        TState currentState,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is TAction typedAction)
        {
            return HandleAsync(typedAction, currentState, cancellationToken);
        }

        return AsyncEnumerable.Empty<IAction>();
    }

    /// <summary>
    ///     Handles the action and optionally yields additional actions.
    /// </summary>
    /// <param name="action">The action that was dispatched.</param>
    /// <param name="currentState">The current feature state after reducers have run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of additional actions to dispatch.</returns>
    public abstract IAsyncEnumerable<IAction> HandleAsync(
        TAction action,
        TState currentState,
        CancellationToken cancellationToken
    );
}