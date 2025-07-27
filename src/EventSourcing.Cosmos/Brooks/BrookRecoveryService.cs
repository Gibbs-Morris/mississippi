using Microsoft.Extensions.Options;

using Mississippi.Core.Abstractions.Brooks;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;


namespace Mississippi.EventSourcing.Cosmos.Brooks;

/// <summary>
///     Service for recovering and managing brook head positions in Cosmos DB.
/// </summary>
internal class BrookRecoveryService : IBrookRecoveryService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookRecoveryService" /> class.
    /// </summary>
    /// <param name="repository">The Cosmos repository for low-level operations.</param>
    /// <param name="retryPolicy">The retry policy for handling transient failures.</param>
    /// <param name="lockManager">The distributed lock manager for concurrency control.</param>
    /// <param name="options">The configuration options for brook storage.</param>
    public BrookRecoveryService(
        ICosmosRepository repository,
        IRetryPolicy retryPolicy,
        IDistributedLockManager lockManager,
        IOptions<BrookStorageOptions> options
    )
    {
        Repository = repository;
        RetryPolicy = retryPolicy;
        LockManager = lockManager;
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private ICosmosRepository Repository { get; }

    private IRetryPolicy RetryPolicy { get; }

    private IDistributedLockManager LockManager { get; }

    private BrookStorageOptions Options { get; }

    /// <summary>
    ///     Gets the current head position for a brook, or recovers it if necessary.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The current or recovered head position of the brook.</returns>
    public async Task<BrookPosition> GetOrRecoverHeadPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        HeadStorageModel? headDocument = await RetryPolicy.ExecuteAsync(
            async () => await Repository.GetHeadDocumentAsync(brookId, cancellationToken),
            cancellationToken);
        if (headDocument == null)
        {
            HeadStorageModel? pendingHead = await RetryPolicy.ExecuteAsync(
                async () => await Repository.GetPendingHeadDocumentAsync(brookId, cancellationToken),
                cancellationToken);
            if (pendingHead != null)
            {
                await using IDistributedLock recoveryLock = await LockManager.AcquireLockAsync(
                    brookId.ToString(),
                    TimeSpan.FromSeconds(Options.LeaseDurationSeconds),
                    cancellationToken);
                await RecoverFromOrphanedOperationAsync(brookId, pendingHead, cancellationToken);
                headDocument = await RetryPolicy.ExecuteAsync(
                    async () => await Repository.GetHeadDocumentAsync(brookId, cancellationToken),
                    cancellationToken);
            }
        }

        return headDocument?.Position ?? new BrookPosition(-1);
    }

    private async Task RecoverFromOrphanedOperationAsync(
        BrookKey brookId,
        HeadStorageModel pendingHead,
        CancellationToken cancellationToken
    )
    {
        long originalPosition = pendingHead.OriginalPosition?.Value ?? -1;
        long targetPosition = pendingHead.Position.Value;
        bool allEventsExist = await CheckAllEventsExistAsync(
            brookId,
            originalPosition,
            targetPosition,
            cancellationToken);
        if (allEventsExist)
        {
            await Repository.CommitHeadPositionAsync(brookId, targetPosition, cancellationToken);
        }
        else
        {
            await RollbackOrphanedOperationAsync(brookId, originalPosition, targetPosition, cancellationToken);
        }
    }

    private async Task<bool> CheckAllEventsExistAsync(
        BrookKey brookId,
        long originalPosition,
        long targetPosition,
        CancellationToken cancellationToken
    )
    {
        if ((targetPosition - originalPosition) > 10)
        {
            ISet<long> existingPositions = await Repository.GetExistingEventPositionsAsync(
                brookId,
                originalPosition + 1,
                targetPosition,
                cancellationToken);
            long expectedCount = targetPosition - originalPosition;
            return existingPositions.Count == expectedCount;
        }

        for (long pos = originalPosition + 1; pos <= targetPosition; pos++)
        {
            if (!await Repository.EventExistsAsync(brookId, pos, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    private async Task RollbackOrphanedOperationAsync(
        BrookKey brookId,
        long originalPosition,
        long targetPosition,
        CancellationToken cancellationToken
    )
    {
        for (long pos = originalPosition + 1; pos <= targetPosition; pos++)
        {
            await RetryPolicy.ExecuteAsync(
                async () =>
                {
                    await Repository.DeleteEventAsync(brookId, pos, cancellationToken);
                    return true;
                },
                cancellationToken);
        }

        await RetryPolicy.ExecuteAsync(
            async () =>
            {
                await Repository.DeletePendingHeadAsync(brookId, cancellationToken);
                return true;
            },
            cancellationToken);
    }
}