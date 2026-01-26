using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Base class for saga compensations. Each compensation has a single <see cref="CompensateAsync" /> method
///     that undoes the work performed by its associated step.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <remarks>
///     <para>
///         Compensations should be stateless and registered in the DI container.
///         Dependencies are injected via the constructor following the get-only property pattern.
///     </para>
///     <para>
///         Compensations are executed in reverse step order when the saga needs to roll back.
///         They should be idempotent—safe to call multiple times with the same result.
///     </para>
///     <para>
///         Associate a compensation with its step using <see cref="SagaCompensationAttribute" />.
///     </para>
/// </remarks>
public abstract class SagaCompensationBase<TSaga>
    where TSaga : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaCompensationBase{TSaga}" /> class.
    /// </summary>
    protected SagaCompensationBase()
    {
    }

    /// <summary>
    ///     Compensates (undoes) the work performed by the associated step.
    /// </summary>
    /// <param name="state">The current saga state.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    ///     A <see cref="CompensationResult" /> indicating success, skip, or failure.
    /// </returns>
    public abstract Task<CompensationResult> CompensateAsync(
        TSaga state,
        CancellationToken cancellationToken
    );
}
