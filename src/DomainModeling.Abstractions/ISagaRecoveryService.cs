using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Exposes framework-managed saga runtime-status and manual-resume operations.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaRecoveryService<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Gets the raw saga state for the specified saga.
    /// </summary>
    /// <param name="entityId">The saga entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The raw saga state, or <c>null</c> when the saga is not found.</returns>
    Task<TSaga?> GetStateAsync(
        string entityId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Gets the metadata-only runtime status for the specified saga.
    /// </summary>
    /// <param name="entityId">The saga entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The current runtime status, or <c>null</c> when the saga is not found.</returns>
    Task<SagaRuntimeStatus?> GetRuntimeStatusAsync(
        string entityId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Requests an explicit manual resume for the specified saga.
    /// </summary>
    /// <param name="entityId">The saga entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The typed resume response, or <c>null</c> when the saga is not found.</returns>
    Task<SagaResumeResponse?> ResumeAsync(
        string entityId,
        CancellationToken cancellationToken = default
    );
}