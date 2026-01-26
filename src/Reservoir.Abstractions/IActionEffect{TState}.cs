using System.Collections.Generic;
using System.Threading;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Handles asynchronous side effects triggered by actions for a specific feature state.
/// </summary>
/// <typeparam name="TState">The feature state type this effect is registered for.</typeparam>
/// <remarks>
///     <para>
///         State-scoped action effects run after reducers have processed an action for their
///         feature state. They can perform async operations like HTTP calls, timers, or navigation,
///         and emit new actions as results.
///     </para>
///     <para>
///         Effects receive the current state for reference but should NOT make decisions based on it.
///         All needed data should be extracted from the action itself. This ensures effects remain
///         predictable and testable.
///     </para>
///     <para>
///         For effects that don't need to yield additional actions, inherit from
///         <see cref="SimpleActionEffectBase{TAction,TState}" /> instead.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed class SaveDataEffect : ActionEffectBase&lt;SaveDataAction, AppState&gt;
///         {
///             public override async IAsyncEnumerable&lt;IAction&gt; HandleAsync(
///                 SaveDataAction action,
///                 AppState currentState,
///                 [EnumeratorCancellation] CancellationToken cancellationToken)
///             {
///                 yield return new SavingAction();
///                 await SaveToStorageAsync(action.Data, cancellationToken);
///                 yield return new SavedAction();
///             }
///         }
///     </code>
/// </example>
public interface IActionEffect<in TState>
    where TState : class, IFeatureState
{
    /// <summary>
    ///     Determines whether this effect can handle the given action.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns><see langword="true" /> if this effect handles the action; otherwise, <see langword="false" />.</returns>
    bool CanHandle(
        IAction action
    );

    /// <summary>
    ///     Handles the action asynchronously and yields resulting actions.
    /// </summary>
    /// <param name="action">The action to handle.</param>
    /// <param name="currentState">
    ///     The current feature state after reducers have run. Provided for interface consistency
    ///     and future extensibility, but implementations should prefer extracting data from the action.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of actions to dispatch.</returns>
    /// <remarks>
    ///     <para>
    ///         The state parameter is included for pattern alignment with server-side event effects
    ///         and to support scenarios where effects legitimately need aggregate context.
    ///     </para>
    ///     <para>
    ///         However, for most use cases, effects should extract all needed data from the action
    ///         itself to remain pure and testable.
    ///     </para>
    /// </remarks>
    IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        TState currentState,
        CancellationToken cancellationToken
    );
}