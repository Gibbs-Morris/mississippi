using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure;

using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Locking;
using Mississippi.EventSourcing.Brooks.Cosmos.Retry;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Brooks;

/// <summary>
///     Service for recovering and managing brook cursor positions in Cosmos DB.
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

    private IDistributedLockManager LockManager { get; }

    private BrookStorageOptions Options { get; }

    private ICosmosRepository Repository { get; }

    private IRetryPolicy RetryPolicy { get; }

    /// <summary>
    ///     Gets the current cursor position for a brook, or recovers it if necessary.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The current or recovered cursor position of the brook.</returns>
    public async Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        CursorStorageModel? cursorDocument = await RetryPolicy.ExecuteAsync(
            async () => await Repository.GetCursorDocumentAsync(brookId, cancellationToken),
            cancellationToken);
        if (cursorDocument == null)
        {
            CursorStorageModel? pendingCursor = await RetryPolicy.ExecuteAsync(
                async () => await Repository.GetPendingCursorDocumentAsync(brookId, cancellationToken),
                cancellationToken);
            if (pendingCursor != null)
            {
                // Use a shorter timeout for recovery lock to prevent deadlocks
                TimeSpan recoveryTimeout = TimeSpan.FromSeconds(Math.Min(Options.LeaseDurationSeconds, 30));
                try
                {
                    await using IDistributedLock recoveryLock = await LockManager.AcquireLockAsync(
                        $"recovery-{brookId}", // Use different lock key for recovery
                        recoveryTimeout,
                        cancellationToken);
                    await RecoverFromOrphanedOperationAsync(brookId, pendingCursor, cancellationToken);
                    cursorDocument = await RetryPolicy.ExecuteAsync(
                        async () => await Repository.GetCursorDocumentAsync(brookId, cancellationToken),
                        cancellationToken);
                }
                catch (RequestFailedException)
                {
                    // If we can't acquire the recovery lock, assume another process is handling recovery
                    // Wait a bit and try to read the cursor again
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                    cursorDocument = await RetryPolicy.ExecuteAsync(
                        async () => await Repository.GetCursorDocumentAsync(brookId, cancellationToken),
                        cancellationToken);

                    // If the cursor is still null after waiting, we have a problem
                    if (cursorDocument == null)
                    {
                        throw new InvalidOperationException(
                            $"Unable to recover cursor position for brook {brookId}. " +
                            "Another recovery operation may be in progress or has failed.");
                    }
                }
            }
        }

        return cursorDocument?.Position ?? new BrookPosition(-1);
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

    private async Task RecoverFromOrphanedOperationAsync(
        BrookKey brookId,
        CursorStorageModel pendingCursor,
        CancellationToken cancellationToken
    )
    {
        long originalPosition = pendingCursor.OriginalPosition?.Value ?? -1;
        long targetPosition = pendingCursor.Position.Value;
        bool allEventsExist = await CheckAllEventsExistAsync(
            brookId,
            originalPosition,
            targetPosition,
            cancellationToken);
        if (allEventsExist)
        {
            await Repository.CommitCursorPositionAsync(brookId, targetPosition, cancellationToken);
        }
        else
        {
            await RollbackOrphanedOperationAsync(brookId, originalPosition, targetPosition, cancellationToken);
        }
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
                await Repository.DeletePendingCursorAsync(brookId, cancellationToken);
                return true;
            },
            cancellationToken);
    }
}