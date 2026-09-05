using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>
///     Provides functionality for recovering and managing brook cursor positions in Cosmos DB.
/// </summary>
internal interface IBrookRecoveryService
{
    /// <summary>
    ///     Gets the current cursor position for a brook, or recovers it if necessary.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The current or recovered cursor position of the brook.</returns>
    Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Resolves pending writes while the caller holds the brook writer lock.
    /// </summary>
    /// <param name="brookId">The brook being recovered.</param>
    /// <param name="writerLock">The caller-owned lock for this brook.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authoritative committed cursor after recovery.</returns>
    Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        IDistributedLock writerLock,
        CancellationToken cancellationToken = default
    );
}