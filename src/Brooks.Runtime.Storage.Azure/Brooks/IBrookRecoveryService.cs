using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Locking;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Brooks;

/// <summary>
///     Recovers Brooks Azure pending-write state before reads or new writes proceed.
/// </summary>
internal interface IBrookRecoveryService
{
    /// <summary>
    ///     Resolves the current committed cursor position, performing recovery when needed.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous recovery operation.</returns>
    Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Resolves the current committed cursor position using an already-acquired distributed lock.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="distributedLock">The distributed lock guarding the stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous recovery operation.</returns>
    Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        IDistributedLock distributedLock,
        CancellationToken cancellationToken = default
    );
}