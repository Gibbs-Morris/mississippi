using System.Collections.Generic;
using System.Threading;

using Mississippi.Ripples.Abstractions.Actions;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Handles asynchronous side effects triggered by actions.
/// </summary>
/// <remarks>
///     <para>
///         Effects run after reducers have processed an action. They can perform async operations
///         like HTTP calls, SignalR subscriptions, or timers, and emit new actions as results.
///     </para>
///     <para>
///         Effects return <see cref="IAsyncEnumerable{T}" /> to support streaming multiple actions
///         over time (e.g., progress updates, real-time events).
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed class ProjectionSubscriptionEffect : IEffect
///         {
///             public bool CanHandle(IAction action) => action is SubscribeToProjectionAction;
///
///             public async IAsyncEnumerable&lt;IAction&gt; HandleAsync(
///                 IAction action,
///                 [EnumeratorCancellation] CancellationToken cancellationToken)
///             {
///                 var subscribe = (SubscribeToProjectionAction)action;
///                 yield return new ProjectionLoadingAction(subscribe.EntityId);
///
///                 var data = await LoadFromServer(subscribe.EntityId, cancellationToken);
///                 yield return new ProjectionLoadedAction(subscribe.EntityId, data);
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
    ///     Handles the action and yields resulting actions.
    /// </summary>
    /// <param name="action">The action to handle.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async enumerable of resulting actions to dispatch.</returns>
    IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        CancellationToken cancellationToken = default
    );
}