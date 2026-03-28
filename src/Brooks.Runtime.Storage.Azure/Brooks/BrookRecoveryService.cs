using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Locking;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Brooks;

/// <summary>
///     Recovers Brooks Azure pending-write state before reads and appends proceed.
/// </summary>
internal sealed class BrookRecoveryService : IBrookRecoveryService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookRecoveryService" /> class.
    /// </summary>
    /// <param name="repository">The Brooks Azure repository.</param>
    /// <param name="lockManager">The distributed lock manager.</param>
    /// <param name="options">The Brooks Azure storage options.</param>
    /// <param name="timeProvider">The time provider used for lease-window checks.</param>
    public BrookRecoveryService(
        IAzureBrookRepository repository,
        IDistributedLockManager lockManager,
        IOptions<BrookStorageOptions> options,
        TimeProvider? timeProvider = null
    )
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        LockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    private IDistributedLockManager LockManager { get; }

    private BrookStorageOptions Options { get; }

    private IAzureBrookRepository Repository { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        AzureBrookPendingWriteState? pendingWrite = await Repository.GetPendingWriteAsync(brookId, cancellationToken)
            .ConfigureAwait(false);
        if (pendingWrite == null)
        {
            AzureBrookCommittedCursorState? cursor = await Repository.GetCommittedCursorAsync(brookId, cancellationToken)
                .ConfigureAwait(false);
            return new BrookPosition(cursor?.Position ?? -1);
        }

        await using IDistributedLock distributedLock = await LockManager.AcquireLockAsync(
                brookId,
                TimeSpan.FromSeconds(Options.LeaseDurationSeconds),
                cancellationToken)
            .ConfigureAwait(false);

        return await GetOrRecoverCursorPositionAsync(brookId, distributedLock, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BrookPosition> GetOrRecoverCursorPositionAsync(
        BrookKey brookId,
        IDistributedLock distributedLock,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(distributedLock);

        AzureBrookCommittedCursorState? cursor = await Repository.GetCommittedCursorAsync(brookId, cancellationToken)
            .ConfigureAwait(false);
        AzureBrookPendingWriteState? pendingWrite = await Repository.GetPendingWriteAsync(brookId, cancellationToken)
            .ConfigureAwait(false);

        if (pendingWrite == null)
        {
            return new BrookPosition(cursor?.Position ?? -1);
        }

        EnsureSafeRecoveryWindow(brookId, distributedLock, pendingWrite);

        if ((cursor != null) && (cursor.Position >= pendingWrite.TargetPosition))
        {
            await ResolveCommittedCursorAsync(brookId, cursor, pendingWrite, cancellationToken).ConfigureAwait(false);
            return new BrookPosition(cursor.Position);
        }

        bool allEventsExist = true;
        for (long position = pendingWrite.OriginalPosition + 1; position <= pendingWrite.TargetPosition; position++)
        {
            if (!await Repository.EventExistsAsync(brookId, position, cancellationToken).ConfigureAwait(false))
            {
                allEventsExist = false;
                break;
            }
        }

        if (allEventsExist)
        {
            return await CommitPendingWriteAsync(brookId, cursor, pendingWrite, cancellationToken).ConfigureAwait(false);
        }

        return await RollBackPendingWriteAsync(brookId, cursor, pendingWrite, cancellationToken).ConfigureAwait(false);
    }

    private static string CreateAmbiguousRecoveryMessage(
        BrookKey brookId,
        AzureBrookPendingWriteState pendingWrite
    ) =>
        $"Brooks Azure recovery could not safely resolve pending append '{pendingWrite.AttemptId}' for brook '{brookId}'. Re-read committed state before retrying the logical command.";

    private static string CreateRetryableRecoveryMessage(
        BrookKey brookId,
        AzureBrookPendingWriteState pendingWrite
    ) =>
        $"Brooks Azure recovery cannot safely mutate brook '{brookId}' while pending append '{pendingWrite.AttemptId}' may still belong to a live writer. Retry after re-reading committed state.";

    private async Task<BrookPosition> CommitPendingWriteAsync(
        BrookKey brookId,
        AzureBrookCommittedCursorState? cursor,
        AzureBrookPendingWriteState pendingWrite,
        CancellationToken cancellationToken
    )
    {
        bool advanced = await Repository.TryAdvanceCommittedCursorAsync(
                brookId,
                cursor,
                pendingWrite,
                cancellationToken)
            .ConfigureAwait(false);

        if (!advanced)
        {
            AzureBrookCommittedCursorState? refreshedCursor = await Repository.GetCommittedCursorAsync(brookId, cancellationToken)
                .ConfigureAwait(false);
            AzureBrookPendingWriteState? refreshedPending = await Repository.GetPendingWriteAsync(brookId, cancellationToken)
                .ConfigureAwait(false);

            if ((refreshedCursor != null) && (refreshedCursor.Position >= pendingWrite.TargetPosition))
            {
                await ResolveCommittedCursorAsync(brookId, refreshedCursor, pendingWrite, cancellationToken)
                    .ConfigureAwait(false);
                return new BrookPosition(refreshedCursor.Position);
            }

            if (refreshedPending == null)
            {
                return new BrookPosition(refreshedCursor?.Position ?? pendingWrite.TargetPosition);
            }

            throw new BrookStorageAmbiguousOutcomeException(CreateAmbiguousRecoveryMessage(brookId, refreshedPending));
        }

        await Repository.TryDeletePendingWriteAsync(brookId, pendingWrite, cancellationToken).ConfigureAwait(false);
        return new BrookPosition(pendingWrite.TargetPosition);
    }

    private void EnsureSafeRecoveryWindow(
        BrookKey brookId,
        IDistributedLock distributedLock,
        AzureBrookPendingWriteState pendingWrite
    )
    {
        if (string.Equals(pendingWrite.LeaseId, distributedLock.LockId, StringComparison.Ordinal))
        {
            return;
        }

        TimeSpan elapsed = TimeProvider.GetUtcNow() - pendingWrite.CreatedUtc;
        if (elapsed < TimeSpan.FromSeconds(Options.LeaseDurationSeconds))
        {
            throw new BrookStorageRetryableException(CreateRetryableRecoveryMessage(brookId, pendingWrite));
        }
    }

    private async Task<BrookPosition> ResolveCommittedCursorAsync(
        BrookKey brookId,
        AzureBrookCommittedCursorState cursor,
        AzureBrookPendingWriteState pendingWrite,
        CancellationToken cancellationToken
    )
    {
        bool deleted = await Repository.TryDeletePendingWriteAsync(brookId, pendingWrite, cancellationToken).ConfigureAwait(false);
        if (deleted)
        {
            return new BrookPosition(cursor.Position);
        }

        AzureBrookPendingWriteState? refreshedPending = await Repository.GetPendingWriteAsync(brookId, cancellationToken)
            .ConfigureAwait(false);
        if ((refreshedPending == null) || (cursor.Position >= refreshedPending.TargetPosition))
        {
            return new BrookPosition(cursor.Position);
        }

        throw new BrookStorageAmbiguousOutcomeException(CreateAmbiguousRecoveryMessage(brookId, refreshedPending));
    }

    private async Task<BrookPosition> RollBackPendingWriteAsync(
        BrookKey brookId,
        AzureBrookCommittedCursorState? cursor,
        AzureBrookPendingWriteState pendingWrite,
        CancellationToken cancellationToken
    )
    {
        for (long position = pendingWrite.OriginalPosition + 1; position <= pendingWrite.TargetPosition; position++)
        {
            await Repository.DeleteEventIfExistsAsync(brookId, position, cancellationToken).ConfigureAwait(false);
        }

        bool deleted = await Repository.TryDeletePendingWriteAsync(brookId, pendingWrite, cancellationToken).ConfigureAwait(false);
        if (deleted)
        {
            return new BrookPosition(cursor?.Position ?? -1);
        }

        AzureBrookCommittedCursorState? refreshedCursor = await Repository.GetCommittedCursorAsync(brookId, cancellationToken)
            .ConfigureAwait(false);
        AzureBrookPendingWriteState? refreshedPending = await Repository.GetPendingWriteAsync(brookId, cancellationToken)
            .ConfigureAwait(false);

        if ((refreshedCursor != null) && (refreshedCursor.Position >= pendingWrite.TargetPosition))
        {
            return new BrookPosition(refreshedCursor.Position);
        }

        if (refreshedPending == null)
        {
            return new BrookPosition(refreshedCursor?.Position ?? cursor?.Position ?? -1);
        }

        throw new BrookStorageAmbiguousOutcomeException(CreateAmbiguousRecoveryMessage(brookId, refreshedPending));
    }
}