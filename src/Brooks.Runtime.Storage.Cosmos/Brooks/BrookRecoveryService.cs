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

    private static async Task CheckOwnershipAsync(
        IDistributedLock? writerLock,
        Action? beforeDispatch,
        CancellationToken cancellationToken
    )
    {
        beforeDispatch?.Invoke();
        cancellationToken.ThrowIfCancellationRequested();
        if (writerLock is not null)
        {
            await writerLock.RenewAsync(cancellationToken);
        }
    }

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
                async () => { return await Repository.GetPendingCursorDocumentAsync(brookId, cancellationToken); },
                cancellationToken);
            if (pendingCursor is null)
            {
                CursorStorageModel? cursorDocument = await RetryPolicy.ExecuteAsync(
                    async () => { return await Repository.GetCursorDocumentAsync(brookId, cancellationToken); },
                    cancellationToken);
                BrookPosition position = cursorDocument?.Position ?? new BrookPosition(-1);
                Logger.CursorPositionReturned(brookId, position.Value);
                return position;
            }

            Logger.AcquiringRecoveryLock(brookId, Options.LeaseDurationSeconds);
            IDistributedLock writerLock;
            try
            {
                writerLock = await LockManager.AcquireLockAsync(
                    brookId.ToString(),
                    TimeSpan.FromSeconds(Options.LeaseDurationSeconds),
                    cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                Logger.RecoveryLockFailed(exception, brookId);
                CursorStorageModel? committed = await RetryPolicy.ExecuteAsync(
                    async () => await Repository.GetCursorDocumentAsync(brookId, cancellationToken),
                    cancellationToken);
                BrookPosition snapshot = committed?.Position ?? new BrookPosition(-1);
                Logger.CursorPositionReturned(brookId, snapshot.Value);
                return snapshot;
            }

            await using (writerLock)
            {
                return await ReconcileUnderWriterLockAsync(brookId, writerLock, null, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            Logger.RecoveryFailed(exception, brookId);
            throw;
        }
    }

    /// <summary>
    ///     Reconciles pending evidence while the caller supervises its append lease.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="beforeDispatch">The append ownership check, or null for a standalone call.</param>
    /// <param name="cancellationToken">The append cancellation token.</param>
    /// <returns>The current or recovered cursor position.</returns>
    public async Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        Action? beforeDispatch,
        CancellationToken cancellationToken
    )
    {
        if (beforeDispatch is null)
        {
            return await GetOrRecoverCursorPositionAsync(brookId, cancellationToken);
        }

        Logger.GettingOrRecoveringCursor(brookId);
        try
        {
            return await ReconcileUnderWriterLockAsync(brookId, null, beforeDispatch, cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.RecoveryFailed(exception, brookId);
            throw;
        }
    }

    private async Task<bool> CheckAllEventsExistAsync(
        BrookKey brookId,
        long originalPosition,
        long targetPosition,
        Action? beforeDispatch,
        CancellationToken cancellationToken
    )
    {
        Logger.CheckingEventsExist(brookId, originalPosition, targetPosition);
        if ((targetPosition - originalPosition) > 10)
        {
            beforeDispatch?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            ISet<long> existingPositions = await Repository.GetExistingEventPositionsAsync(
                brookId,
                originalPosition + 1,
                targetPosition,
                cancellationToken);
            long expectedCount = targetPosition - originalPosition;
            return existingPositions.Count == expectedCount;
        }

        for (long pos = originalPosition; pos < targetPosition;)
        {
            pos++;
            beforeDispatch?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            if (!await Repository.EventExistsAsync(brookId, pos, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<BrookPosition> ReconcileUnderWriterLockAsync(
        BrookKey brookId,
        IDistributedLock? writerLock,
        Action? beforeDispatch,
        CancellationToken cancellationToken
    )
    {
        await CheckOwnershipAsync(writerLock, beforeDispatch, cancellationToken);
        CursorStorageModel? pendingCursor = await RetryPolicy.ExecuteAsync(
            async () =>
            {
                beforeDispatch?.Invoke();
                return await Repository.GetPendingCursorDocumentAsync(brookId, cancellationToken);
            },
            cancellationToken);
        CursorStorageModel? cursorDocument = await RetryPolicy.ExecuteAsync(
            async () =>
            {
                beforeDispatch?.Invoke();
                return await Repository.GetCursorDocumentAsync(brookId, cancellationToken);
            },
            cancellationToken);
        await CheckOwnershipAsync(writerLock, beforeDispatch, cancellationToken);
        BrookPosition position = cursorDocument?.Position ?? new BrookPosition(-1);
        if (pendingCursor is null)
        {
            Logger.CursorPositionReturned(brookId, position.Value);
            return position;
        }

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

        if (string.IsNullOrWhiteSpace(pendingCursor.ETag))
        {
            throw new InvalidOperationException($"Pending cursor for brook {brookId} has no attempt entity tag.");
        }

        await CheckOwnershipAsync(writerLock, beforeDispatch, cancellationToken);
        bool hasAllEvents = await CheckAllEventsExistAsync(
            brookId,
            originalPosition,
            targetPosition,
            beforeDispatch,
            cancellationToken);
        await CheckOwnershipAsync(writerLock, beforeDispatch, cancellationToken);
        if (!hasAllEvents)
        {
            // A missing event does not fence a create request still in flight after the old lease expired.
            throw new InvalidOperationException(
                $"Pending append for brook {brookId} is incomplete; its outcome remains unresolved and recovery metadata is retained.");
        }

        if (committedPosition >= targetPosition)
        {
            // A lost commit acknowledgement must never cause committed events to be deleted.
            await RetryPolicy.ExecuteAsync(
                async () =>
                {
                    await CheckOwnershipAsync(writerLock, beforeDispatch, cancellationToken);
                    if (beforeDispatch is null)
                    {
                        await Repository.DeletePendingCursorAsync(brookId, pendingCursor.ETag, cancellationToken);
                    }
                    else
                    {
                        await Repository.DeletePendingCursorAsync(
                            brookId,
                            pendingCursor.ETag,
                            beforeDispatch,
                            cancellationToken);
                    }

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
                beforeDispatch,
                cancellationToken);
        }

        Logger.CursorPositionReturned(brookId, position.Value);
        return position;
    }

    private async Task<BrookPosition> RecoverFromOrphanedOperationAsync(
        BrookKey brookId,
        CursorStorageModel pendingCursor,
        IDistributedLock? writerLock,
        Action? beforeDispatch,
        CancellationToken cancellationToken
    )
    {
        long targetPosition = pendingCursor.Position.Value;
        await CheckOwnershipAsync(writerLock, beforeDispatch, cancellationToken);
        Logger.RecoveryCommitting(brookId, targetPosition);
        if (beforeDispatch is null)
        {
            await Repository.CommitCursorPositionAsync(brookId, targetPosition, pendingCursor.ETag!, cancellationToken);
        }
        else
        {
            await Repository.CommitCursorPositionAsync(
                brookId,
                targetPosition,
                pendingCursor.ETag!,
                beforeDispatch,
                cancellationToken);
        }

        return new(targetPosition);
    }
}