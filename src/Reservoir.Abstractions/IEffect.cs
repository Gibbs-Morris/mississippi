using System.Collections.Generic;
using System.Threading;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Handles asynchronous side effects triggered by actions.
/// </summary>
/// <remarks>
///     <para>
///         Effects run after reducers have processed an action. They can perform async operations
///         like HTTP calls, timers, or navigation, and emit new actions as results.
///     </para>
///     <para>
///         Effects return <see cref="IAsyncEnumerable{T}" /> to support streaming multiple actions
///         over time (e.g., progress updates, polling results).
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed class SaveDataEffect : IEffect
///         {
///             public bool CanHandle(IAction action) => action is SaveDataAction;
///
///             public async IAsyncEnumerable&lt;IAction&gt; HandleAsync(
///                 IAction action,
///                 [EnumeratorCancellation] CancellationToken cancellationToken)
///             {
///                 var save = (SaveDataAction)action;
///                 yield return new SavingAction();
///
///                 await SaveToStorageAsync(save.Data, cancellationToken);
///                 yield return new SavedAction();
///             }
///         }
///     </code>
/// </example>
public interface IEffect
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
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of actions to dispatch.</returns>
    IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        CancellationToken cancellationToken
    );
}