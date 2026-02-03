using System.Threading;
using System.Threading.Tasks;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Executes a saga step against the current saga state.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaStep<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Executes the step against the current saga state.
    /// </summary>
    /// <param name="state">The current saga state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The step execution result.</returns>
    Task<StepResult> ExecuteAsync(TSaga state, CancellationToken cancellationToken);
}

/// <summary>
///     Supports compensation for a saga step.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ICompensatable<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Compensates the effects of the step against the current saga state.
    /// </summary>
    /// <param name="state">The current saga state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compensation result.</returns>
    Task<CompensationResult> CompensateAsync(TSaga state, CancellationToken cancellationToken);
}
