using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Base class for saga steps. Each step has a single <see cref="ExecuteAsync" /> method,
///     mirroring the <c>CommandHandlerBase</c> pattern.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <remarks>
///     <para>
///         Steps should be stateless and registered in the DI container.
///         Dependencies are injected via the constructor following the get-only property pattern.
///     </para>
///     <para>
///         Lifecycle events (started/completed/failed) are emitted automatically by the saga
///         infrastructure. Steps only need to return business events when the saga state needs updating.
///     </para>
/// </remarks>
public abstract class SagaStepBase<TSaga>
    where TSaga : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepBase{TSaga}" /> class.
    /// </summary>
    protected SagaStepBase()
    {
    }

    /// <summary>
    ///     Executes the step action against the current saga state.
    /// </summary>
    /// <param name="state">The current saga state.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    ///     A <see cref="StepResult" /> indicating success or failure,
    ///     optionally containing business events to persist.
    /// </returns>
    public abstract Task<StepResult> ExecuteAsync(
        TSaga state,
        CancellationToken cancellationToken
    );
}
