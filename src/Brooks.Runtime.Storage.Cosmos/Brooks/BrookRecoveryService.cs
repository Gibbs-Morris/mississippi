using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Common.Cosmos.Abstractions.Retry;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Locking;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Brooks;

/// <summary>
///     Service for recovering and managing brook cursor positions in Cosmos DB.
/// </summary>
internal sealed class BrookRecoveryService : IBrookRecoveryService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookRecoveryService" /> class.
    /// </summary>
    /// <param name="repository">The Cosmos repository for low-level operations.</param>
    /// <param name="retryPolicy">The retry policy for handling transient failures.</param>
    /// <param name="lockManager">The distributed lock manager for concurrency control.</param>
    /// <param name="options">The configuration options for brook storage.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public BrookRecoveryService(
        ICosmosRepository repository,
        IRetryPolicy retryPolicy,
        IDistributedLockManager lockManager,
        IOptions<BrookStorageOptions> options,
        ILogger<BrookRecoveryService> logger
    )
    {
        Repository = repository;
        RetryPolicy = retryPolicy;
        LockManager = lockManager;
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger;
    }

    private IDistributedLockManager LockManager { get; }

    private ILogger<BrookRecoveryService> Logger { get; }

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
        Logger.GettingOrRecoveringCursor(brookId);
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
                long originalPos = pendingCursor.OriginalPosition?.Value ?? -1;
                long targetPos = pendingCursor.Position.Value;
                Logger.PendingCursorDetected(brookId, originalPos, targetPos);

                // Use a shorter timeout for recovery lock to prevent deadlocks
                TimeSpan recoveryTimeout = TimeSpan.FromSeconds(Math.Min(Options.LeaseDurationSeconds, 30));
                Logger.AcquiringRecoveryLock(brookId, recoveryTimeout.TotalSeconds);
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
                catch (RequestFailedException ex)
                {
                    Logger.RecoveryLockFailed(ex, brookId);

                    // If we can't acquire the recovery lock, assume another process is handling recovery
                    // Wait a bit and try to read the cursor again
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                    cursorDocument = await RetryPolicy.ExecuteAsync(
                        async () => await Repository.GetCursorDocumentAsync(brookId, cancellationToken),
                        cancellationToken);

                    // If the cursor is still null after waiting, we have a problem
                    if (cursorDocument == null)
                    {
                        InvalidOperationException invalidOperationException = new(
                            $"Unable to recover cursor position for brook {brookId}. " +
                            "Another recovery operation may be in progress or has failed.");
                        Logger.RecoveryFailed(invalidOperationException, brookId);
                        throw invalidOperationException;
                    }
                }
            }
        }

        BrookPosition position = cursorDocument?.Position ?? new BrookPosition(-1);
        Logger.CursorPositionReturned(brookId, position.Value);
        return position;
    }

    private async Task<bool> CheckAllEventsExistAsync(
        BrookKey brookId,
        long originalPosition,
        long targetPosition,
        CancellationToken cancellationToken
    )
    {
        Logger.CheckingEventsExist(brookId, originalPosition, targetPosition);
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
            Logger.RecoveryCommitting(brookId, targetPosition);
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
        Logger.RollingBack(brookId, originalPosition, targetPosition);
        long eventsDeleted = 0;
        for (long pos = originalPosition + 1; pos <= targetPosition; pos++)
        {
            Logger.DeletingOrphanedEvent(brookId, pos);
            await RetryPolicy.ExecuteAsync(
                async () =>
                {
                    await Repository.DeleteEventAsync(brookId, pos, cancellationToken);
                    return true;
                },
                cancellationToken);
            eventsDeleted++;
        }

        await RetryPolicy.ExecuteAsync(
            async () =>
            {
                await Repository.DeletePendingCursorAsync(brookId, cancellationToken);
                return true;
            },
            cancellationToken);
        Logger.RollbackCompleted(brookId, eventsDeleted);
    }
}