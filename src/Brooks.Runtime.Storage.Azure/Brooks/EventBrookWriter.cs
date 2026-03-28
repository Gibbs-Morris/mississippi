using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Locking;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Brooks;

/// <summary>
///     Appends Brooks event batches using Azure Blob pending-state and lease coordination.
/// </summary>
internal sealed class EventBrookWriter : IEventBrookWriter
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventBrookWriter" /> class.
    /// </summary>
    /// <param name="repository">The Brooks Azure repository.</param>
    /// <param name="lockManager">The distributed lock manager.</param>
    /// <param name="recoveryService">The recovery service.</param>
    /// <param name="options">The Brooks Azure storage options.</param>
    /// <param name="timeProvider">The time provider used for pending-write timestamps.</param>
    public EventBrookWriter(
        IAzureBrookRepository repository,
        IDistributedLockManager lockManager,
        IBrookRecoveryService recoveryService,
        IOptions<BrookStorageOptions> options,
        TimeProvider? timeProvider = null
    )
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        LockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
        RecoveryService = recoveryService ?? throw new ArgumentNullException(nameof(recoveryService));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    private IDistributedLockManager LockManager { get; }

    private BrookStorageOptions Options { get; }

    private IBrookRecoveryService RecoveryService { get; }

    private IAzureBrookRepository Repository { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task<BrookPosition> AppendEventsAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion,
        CancellationToken cancellationToken = default
    )
    {
        if ((events == null) || (events.Count == 0))
        {
            throw new ArgumentException("Events collection cannot be null or empty", nameof(events));
        }

        await using IDistributedLock distributedLock = await LockManager.AcquireLockAsync(
                brookId,
                TimeSpan.FromSeconds(Options.LeaseDurationSeconds),
                cancellationToken)
            .ConfigureAwait(false);

        BrookPosition currentCursor = await RecoveryService.GetOrRecoverCursorPositionAsync(
                brookId,
                distributedLock,
                cancellationToken)
            .ConfigureAwait(false);

        if (expectedVersion.HasValue && (expectedVersion.Value != currentCursor))
        {
            throw new OptimisticConcurrencyException(
                $"Expected version {expectedVersion.Value} but current cursor is {currentCursor}.");
        }

        long finalPosition = checked(currentCursor.Value + events.Count);
        AzureBrookPendingWriteState pendingWrite = new()
        {
            AttemptId = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture),
            WriteEpoch = finalPosition,
            OriginalPosition = currentCursor.Value,
            TargetPosition = finalPosition,
            EventCount = events.Count,
            LeaseId = distributedLock.LockId,
            CreatedUtc = TimeProvider.GetUtcNow(),
        };

        bool pendingCreated = await Repository.TryCreatePendingWriteAsync(brookId, pendingWrite, cancellationToken)
            .ConfigureAwait(false);
        if (!pendingCreated)
        {
            throw new BrookStorageRetryableException(
                $"Brooks Azure append could not create pending state for brook '{brookId}'. Re-read committed state and retry.");
        }

        try
        {
            for (int index = 0; index < events.Count; index++)
            {
                await distributedLock.RenewAsync(cancellationToken).ConfigureAwait(false);
                await Repository.WriteEventAsync(
                        brookId,
                        currentCursor.Value + index + 1,
                        events[index],
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            bool committed = await Repository.TryAdvanceCommittedCursorAsync(
                    brookId,
                    await Repository.GetCommittedCursorAsync(brookId, cancellationToken).ConfigureAwait(false),
                    pendingWrite,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!committed)
            {
                BrookPosition resolvedPosition = await RecoveryService.GetOrRecoverCursorPositionAsync(
                        brookId,
                        distributedLock,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (resolvedPosition.Value >= finalPosition)
                {
                    return new BrookPosition(finalPosition);
                }

                throw new BrookStorageAmbiguousOutcomeException(
                    $"Brooks Azure append could not safely determine whether brook '{brookId}' committed event batch '{pendingWrite.AttemptId}'. Re-read committed state before retrying the logical command.");
            }

            await Repository.TryDeletePendingWriteAsync(brookId, pendingWrite, cancellationToken).ConfigureAwait(false);
            return new BrookPosition(finalPosition);
        }
        catch (BrookStorageAmbiguousOutcomeException)
        {
            throw;
        }
        catch (BrookStorageRetryableException)
        {
            throw;
        }
        catch (OptimisticConcurrencyException)
        {
            throw;
        }
        catch (Exception exception)
        {
            BrookPosition resolvedPosition = await RecoveryService.GetOrRecoverCursorPositionAsync(
                    brookId,
                    distributedLock,
                    cancellationToken)
                .ConfigureAwait(false);

            if (resolvedPosition.Value >= finalPosition)
            {
                return new BrookPosition(finalPosition);
            }

            if (resolvedPosition.Value == currentCursor.Value)
            {
                throw;
            }

            throw new BrookStorageAmbiguousOutcomeException(
                $"Brooks Azure append could not safely determine whether brook '{brookId}' committed event batch '{pendingWrite.AttemptId}'. Re-read committed state before retrying the logical command.",
                exception);
        }
    }
}