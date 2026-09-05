using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;

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
        try
        {
            CursorStorageModel? pendingCursor = await RetryPolicy.ExecuteAsync(
                async () => await Repository.GetPendingCursorDocumentAsync(brookId, cancellationToken),
                cancellationToken);
            if (pendingCursor is null)
            {
                CursorStorageModel? cursorDocument = await RetryPolicy.ExecuteAsync(
                    async () => await Repository.GetCursorDocumentAsync(brookId, cancellationToken),
                    cancellationToken);
                BrookPosition position = cursorDocument?.Position ?? new BrookPosition(-1);
                Logger.CursorPositionReturned(brookId, position.Value);
                return position;
            }

            Logger.AcquiringRecoveryLock(brookId, Options.LeaseDurationSeconds);
            await using IDistributedLock writerLock = await LockManager.AcquireLockAsync(
                brookId.ToString(),
                TimeSpan.FromSeconds(Options.LeaseDurationSeconds),
                cancellationToken);
            return await GetOrRecoverCursorPositionAsync(brookId, writerLock, cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.RecoveryFailed(exception, brookId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        IDistributedLock writerLock,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(writerLock);
        Logger.GettingOrRecoveringCursor(brookId);
        await writerLock.RenewAsync(cancellationToken);
        CursorStorageModel? cursorDocument = await RetryPolicy.ExecuteAsync(
            async () => await Repository.GetCursorDocumentAsync(brookId, cancellationToken),
            cancellationToken);
        CursorStorageModel? pendingCursor = await RetryPolicy.ExecuteAsync(
            async () => await Repository.GetPendingCursorDocumentAsync(brookId, cancellationToken),
            cancellationToken);
        BrookPosition position = cursorDocument?.Position ?? new BrookPosition(-1);
        if (pendingCursor != null)
        {
            long committedPosition = cursorDocument?.Position.Value ?? -1;
            long originalPosition = pendingCursor.OriginalPosition.GetValueOrDefault(new(-1)).Value;
            long targetPosition = pendingCursor.Position.Value;
            Logger.PendingCursorDetected(brookId, originalPosition, targetPosition);
            if ((targetPosition <= originalPosition) ||
                ((committedPosition < targetPosition) && (committedPosition != originalPosition)))
            {
                throw new InvalidOperationException(
                    $"Pending cursor for brook {brookId} does not match its committed history.");
            }

            await writerLock.RenewAsync(cancellationToken);
            if (committedPosition >= targetPosition)
            {
                // A lost commit acknowledgement must never cause committed events to be deleted.
                await RetryPolicy.ExecuteAsync(
                    async () =>
                    {
                        await writerLock.RenewAsync(cancellationToken);
                        await Repository.DeletePendingCursorAsync(brookId, cancellationToken);
                        return true;
                    },
                    cancellationToken);
            }
            else
            {
                position = await RecoverFromOrphanedOperationAsync(
                    brookId,
                    pendingCursor,
                    writerLock,
                    cancellationToken);
            }
        }

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

    private async Task<BrookPosition> RecoverFromOrphanedOperationAsync(
        BrookKey brookId,
        CursorStorageModel pendingCursor,
        IDistributedLock writerLock,
        CancellationToken cancellationToken
    )
    {
        long originalPosition = pendingCursor.OriginalPosition.GetValueOrDefault(new(-1)).Value;
        long targetPosition = pendingCursor.Position.Value;
        bool allEventsExist = await CheckAllEventsExistAsync(
            brookId,
            originalPosition,
            targetPosition,
            cancellationToken);
        if (allEventsExist)
        {
            await writerLock.RenewAsync(cancellationToken);
            Logger.RecoveryCommitting(brookId, targetPosition);
            await Repository.CommitCursorPositionAsync(brookId, targetPosition, cancellationToken);
            return new(targetPosition);
        }

        // A missing event does not fence a create request still in flight after the old lease expired.
        throw new InvalidOperationException(
            $"Pending append for brook {brookId} is incomplete; its outcome remains unresolved and recovery metadata is retained.");
    }
}