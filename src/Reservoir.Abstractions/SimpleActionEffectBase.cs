using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Base class for action effects that perform work without yielding additional actions.
/// </summary>
/// <typeparam name="TAction">The action type this effect handles.</typeparam>
/// <typeparam name="TState">The feature state type this effect is registered for.</typeparam>
/// <remarks>
///     <para>
///         Use this base class when your effect performs side effects (logging, analytics,
///         external API calls) but does not need to dispatch additional actions.
///     </para>
///     <para>
///         Effects should be stateless and registered as transient services.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// public sealed class LogDepositEffect : SimpleActionEffectBase&lt;DepositFundsAction, BankAccountState&gt;
/// {
///     public override Task HandleAsync(
///         DepositFundsAction action,
///         BankAccountState currentState,
///         CancellationToken cancellationToken)
///     {
///         Console.WriteLine($"Deposit: {action.Amount} to {action.AccountId}");
///         return Task.CompletedTask;
///     }
/// }
///     </code>
/// </example>
public abstract class SimpleActionEffectBase<TAction, TState> : IActionEffect<TState>
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
            return HandleCoreAsync(typedAction, currentState, cancellationToken);
        }

        return AsyncEnumerable.Empty<IAction>();
    }

    /// <summary>
    ///     Handles the action without yielding additional actions.
    /// </summary>
    /// <param name="action">The action that was dispatched.</param>
    /// <param name="currentState">The current feature state after reducers have run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task HandleAsync(
        TAction action,
        TState currentState,
        CancellationToken cancellationToken
    );

    private async IAsyncEnumerable<IAction> HandleCoreAsync(
        TAction action,
        TState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await HandleAsync(action, currentState, cancellationToken).ConfigureAwait(false);
        yield break;
    }
}