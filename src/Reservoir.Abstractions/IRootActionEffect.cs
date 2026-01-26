using System.Collections.Generic;
using System.Threading;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Root composite action effect that dispatches to all registered effects for a feature state.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
/// <remarks>
///     <para>
///         Similar to <see cref="IRootReducer{TState}" />, this interface provides a single
///         entry point for dispatching actions to all effects registered for a feature state.
///     </para>
///     <para>
///         The implementation pre-indexes effects by action type for O(1) dispatch.
///     </para>
/// </remarks>
public interface IRootActionEffect<TState>
    where TState : class, IFeatureState
{
    /// <summary>
    ///     Gets the count of registered effects.
    /// </summary>
    /// <remarks>
    ///     This property is provided for parity with <c>IRootEventEffect{TAggregate}</c>
    ///     on the server side.
    /// </remarks>
    int EffectCount { get; }

    /// <summary>
    ///     Gets a value indicating whether any effects are registered for this feature state.
    /// </summary>
    bool HasEffects { get; }

    /// <summary>
    ///     Handles an action by dispatching to all matching effects.
    /// </summary>
    /// <param name="action">The action to handle.</param>
    /// <param name="currentState">The current feature state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     An async enumerable of additional actions yielded by effects.
    /// </returns>
    IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        TState currentState,
        CancellationToken cancellationToken
    );
}