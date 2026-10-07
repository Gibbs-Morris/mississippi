using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;

/// <summary>
///     Cosmos DB implementation of the event brook writer for writing events to brooks.
/// </summary>
internal sealed class EventBrookWriter : IEventBrookWriter
{
    private static readonly Action<ILogger, BrookKey, int, long, long, long, Exception?> LogAppenderSummary =
        LoggerMessage.Define<BrookKey, int, long, long, long>(
            LogLevel.Information,
            new(1001, nameof(AppendEventsAsync)),
            "CosmosAppender: brook={Brook} count={Count} estSize={Size}B cursor={Cursor} -> final={Final}");

    private static readonly Action<ILogger, int, int, BrookKey, int, long, long, Exception?> LogBatchProgress =
        LoggerMessage.Define<int, int, BrookKey, int, long, long>(
            LogLevel.Information,
            new(1005, nameof(AppendLargeBatchAsync)),
            "CosmosAppender: Batch {BatchIdx}/{BatchCount} brook={Brook} count={Count} estSize={Size}B startPos={Start}");

    private static readonly Action<ILogger, BrookKey, long, int, Exception?> LogLargeBatchCommitted =
        LoggerMessage.Define<BrookKey, long, int>(
            LogLevel.Information,
            new(1007, nameof(AppendLargeBatchAsync)),
            "CosmosAppender: LargeBatch committed brook={Brook} newCursor={Cursor} batches={Batches}");

    private static readonly Action<ILogger, BrookKey, int, int, Exception?> LogLargeBatchSummaryPart1 =
        LoggerMessage.Define<BrookKey, int, int>(
            LogLevel.Information,
            new(1003, nameof(AppendLargeBatchAsync)),
            "CosmosAppender: LargeBatch brook={Brook} totalCount={Total} batches={Batches}");

    private static readonly Action<ILogger, long, long, int, long, Exception?> LogLargeBatchSummaryPart2 =
        LoggerMessage.Define<long, long, int, long>(
            LogLevel.Information,
            new(1004, nameof(AppendLargeBatchAsync)),
            "CosmosAppender: LargeBatch cursor={Cursor} final={Final} maxPerBatch={MaxEv} maxReq={MaxReq}");

    private static readonly Action<ILogger, BrookKey, long, long, string, Exception?> LogRollbackFailed =
        LoggerMessage.Define<BrookKey, long, long, string>(
            LogLevel.Error,
            new(1008, nameof(RollbackLargeBatchAsync)),
            "CosmosAppender: Rollback failed brook={Brook} originalCursor={Cursor} failedFinal={Final} remainingEvents={Remaining}");

    private static readonly Action<ILogger, BrookKey, long, Exception?> LogRollbackSucceeded =
        LoggerMessage.Define<BrookKey, long>(
            LogLevel.Warning,
            new(1009, nameof(RollbackLargeBatchAsync)),
            "CosmosAppender: Rollback succeeded brook={Brook} restoredCursor={Cursor}");

    private static readonly Action<ILogger, BrookKey, long, int, double, Exception?> LogSingleBatchCommitted =
        LoggerMessage.Define<BrookKey, long, int, double>(
            LogLevel.Information,
            new(1006, nameof(AppendSingleBatchAsync)),
            "CosmosAppender: SingleBatch committed brook={Brook} newCursor={Cursor} status={Status} charge={Charge}");

    private static readonly Action<ILogger, BrookKey, int, long, long, long, Exception?> LogSingleBatchStart =
        LoggerMessage.Define<BrookKey, int, long, long, long>(
            LogLevel.Information,
            new(1002, nameof(AppendSingleBatchAsync)),
            "CosmosAppender: SingleBatch brook={Brook} count={Count} estSize={Size}B startPos={Start} final={Final}");

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventBrookWriter" /> class.
    /// </summary>
    /// <param name="repository">The Cosmos repository for low-level operations.</param>
    /// <param name="lockManager">The distributed lock manager for concurrency control.</param>
    /// <param name="sizeEstimator">The batch size estimator for optimizing batch operations.</param>
    /// <param name="retryPolicy">The retry policy for handling transient failures.</param>
    /// <param name="options">The configuration options for brook storage.</param>
    /// <param name="eventMapper">The mapper for converting events to storage models.</param>
    /// <param name="recoveryService">The brook recovery service for cursor position management.</param>
    /// <param name="logger">The logger used to record operational diagnostics.</param>
    /// <param name="timeProvider">Time provider for timestamps. If null, uses <see cref="TimeProvider.System" />.</param>
    /// <param name="deadlineScheduler">The internal deadline scheduler, or the default thread-pool scheduler.</param>
    public EventBrookWriter(
        ICosmosRepository repository,
        IDistributedLockManager lockManager,
        IBatchSizeEstimator sizeEstimator,
        IRetryPolicy retryPolicy,
        IOptions<BrookStorageOptions> options,
        IMapper<BrookEvent, EventStorageModel> eventMapper,
        IBrookRecoveryService recoveryService,
        ILogger<EventBrookWriter> logger,
        TimeProvider? timeProvider = null,
        TaskScheduler? deadlineScheduler = null
    )
    {
        Repository = repository;
        LockManager = lockManager;
        SizeEstimator = sizeEstimator;
        RetryPolicy = retryPolicy;
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        EventMapper = eventMapper;
        RecoveryService = recoveryService;
        Logger = logger;
        TimeProvider = timeProvider ?? TimeProvider.System;
        DeadlineScheduler = deadlineScheduler ?? TaskScheduler.Default;
    }

    private TaskScheduler DeadlineScheduler { get; }

    private IMapper<BrookEvent, EventStorageModel> EventMapper { get; }

    private IDistributedLockManager LockManager { get; }

    private ILogger<EventBrookWriter> Logger { get; }

    private BrookStorageOptions Options { get; }

    private IBrookRecoveryService RecoveryService { get; }

    private ICosmosRepository Repository { get; }

    private IRetryPolicy RetryPolicy { get; }

    private IBatchSizeEstimator SizeEstimator { get; }

    private TimeProvider TimeProvider { get; }

    /// <summary>
    ///     Preserves the ownership or caller-cancellation cause instead of its linked-token wrapper.
    /// </summary>
    /// <param name="exception">The failure reported by append work.</param>
    /// <param name="lifetime">The append ownership supervisor.</param>
    /// <param name="cancellationToken">The original caller token.</param>
    /// <returns>The primary failure to retain through shutdown.</returns>
    private static Exception SelectAppendFailure(
        Exception exception,
        AppendLeaseLifetime lifetime,
        CancellationToken cancellationToken
    )
    {
        if (exception is not OperationCanceledException)
        {
            return exception;
        }

        if (lifetime.Failure is Exception leaseFailure)
        {
            return leaseFailure;
        }

        return cancellationToken.IsCancellationRequested
            ? new OperationCanceledException(cancellationToken)
            : exception;
    }

    /// <summary>
    ///     Identifies failures collected in the rollback aggregate while ownership remains healthy.
    /// </summary>
    /// <param name="exception">The rollback request or verification failure.</param>
    /// <returns>Whether the existing rollback failure categories include this exception.</returns>
    private static bool ShouldRecordRollbackFailure(
        Exception exception
    ) =>
        exception is InvalidOperationException or TimeoutException or HttpRequestException;

    /// <summary>
    ///     Appends a collection of events to the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="events">The collection of events to append to the brook.</param>
    /// <param name="expectedVersion">The expected version for optimistic concurrency control.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The position after successfully appending all events.</returns>
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

        // Validate bounds to prevent overflow
        if (events.Count > (int.MaxValue / 2))
        {
            throw new ArgumentException(
                $"Too many events in single batch: {events.Count}. Maximum allowed: {int.MaxValue / 2}",
                nameof(events));
        }

        AppendLeaseLifetime.ValidateOptions(Options);
        Exception? primaryFailure = null;
        BrookPosition? committed = null;
        try
        {
            await using IDistributedLock distributedLock = await LockManager.AcquireLockAsync(
                brookId.ToString(),
                TimeSpan.FromSeconds(Options.LeaseDurationSeconds),
                cancellationToken);
            try
            {
                await using AppendLeaseLifetime lifetime = new(
                    distributedLock,
                    new OptionsWrapper<BrookStorageOptions>(Options),
                    TimeProvider,
                    cancellationToken,
                    DeadlineScheduler);
                try
                {
                    await lifetime.StartAsync();
                    committed = await AppendEventsWithLockAsync(
                        brookId,
                        events,
                        expectedVersion,
                        lifetime,
                        lifetime.CancellationToken);
                    return committed.Value;
                }
                catch (Exception exception)
                {
                    primaryFailure = SelectAppendFailure(exception, lifetime, cancellationToken);
                    throw;
                }
            }
            catch (Exception exception)
            {
                primaryFailure ??= exception;
                throw;
            }
        }
        catch (Exception exception)
        {
            if (committed is BrookPosition position)
            {
                Logger.AppendCleanupFailed(primaryFailure ?? exception, brookId, position.Value);
                return position;
            }

            if (primaryFailure is not null && !ReferenceEquals(primaryFailure, exception))
            {
                ExceptionDispatchInfo.Capture(primaryFailure).Throw();
            }

            throw;
        }
    }

    private async Task<BrookPosition> AppendEventsWithLockAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion,
        AppendLeaseLifetime lifetime,
        CancellationToken cancellationToken
    )
    {
        // Get current cursor position while holding the lock to ensure consistency
        lifetime.ThrowIfFailed();
        BrookPosition currentCursor = await RecoveryService.GetOrRecoverCursorPositionAsync(
            brookId,
            lifetime.ThrowIfFailed,
            cancellationToken);
        lifetime.ThrowIfFailed();

        // Perform optimistic concurrency check inside the lock
        if (expectedVersion.HasValue && (expectedVersion.Value != currentCursor))
        {
            throw new OptimisticConcurrencyException(
                $"Expected version {expectedVersion.Value} but current cursor is {currentCursor}");
        }

        // Check for potential overflow in position calculation
        if (currentCursor.Value > (long.MaxValue - events.Count))
        {
            throw new InvalidOperationException(
                $"Position overflow: current cursor {currentCursor.Value} + {events.Count} events would exceed maximum position");
        }

        long finalPosition = currentCursor.Value + events.Count;
        long estimatedSize = SizeEstimator.EstimateBatchSize(events);
        LogAppenderSummary(Logger, brookId, events.Count, estimatedSize, currentCursor.Value, finalPosition, null);
        if ((events.Count > Options.MaxEventsPerBatch) || (estimatedSize > Options.MaxRequestSizeBytes))
        {
            return await AppendLargeBatchAsync(
                brookId,
                events,
                currentCursor,
                finalPosition,
                lifetime,
                cancellationToken);
        }

        return await AppendSingleBatchAsync(brookId, events, currentCursor, finalPosition, lifetime, cancellationToken);
    }

    private async Task<BrookPosition> AppendLargeBatchAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition currentCursor,
        long finalPosition,
        AppendLeaseLifetime lifetime,
        CancellationToken cancellationToken
    )
    {
        // Build batches first while holding the lock, so we fail fast for oversize events
        List<IReadOnlyList<BrookEvent>> batches = SizeEstimator.CreateSizeLimitedBatches(
                events,
                Options.MaxEventsPerBatch,
                Options.MaxRequestSizeBytes)
            .ToList();
        lifetime.ThrowIfFailed();
        await Repository.CreatePendingCursorAsync(
            brookId,
            currentCursor,
            finalPosition,
            lifetime.ThrowIfFailed,
            cancellationToken);
        int processedEvents = 0;
        try
        {
            LogLargeBatchSummaryPart1(Logger, brookId, events.Count, batches.Count, null);
            LogLargeBatchSummaryPart2(
                Logger,
                currentCursor.Value,
                finalPosition,
                Options.MaxEventsPerBatch,
                Options.MaxRequestSizeBytes,
                null);
            for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
            {
                lifetime.ThrowIfFailed();
                IReadOnlyList<BrookEvent> batchEvents = batches[batchIndex];
                long batchStartPosition = currentCursor.Value + processedEvents + 1;
                long estBatchSize = SizeEstimator.EstimateBatchSize(batchEvents);
                LogBatchProgress(
                    Logger,
                    batchIndex + 1,
                    batches.Count,
                    brookId,
                    batchEvents.Count,
                    estBatchSize,
                    batchStartPosition,
                    null);
                List<EventStorageModel> storageBatchEvents = batchEvents.Select(EventMapper.Map).ToList();
                await Repository.AppendEventBatchAsync(
                    brookId,
                    storageBatchEvents,
                    batchStartPosition,
                    lifetime.ThrowIfFailed,
                    cancellationToken);
                processedEvents += batchEvents.Count;
                lifetime.ThrowIfFailed();
            }
        }
        catch (Exception appendFailure) when (lifetime.CanRollback)
        {
            // Rollback only what we actually appended
            // processedEvents reflects successfully created items
            try
            {
                await RollbackLargeBatchAsync(
                    brookId,
                    new(currentCursor.Value),
                    currentCursor.Value + processedEvents,
                    lifetime,
                    cancellationToken);
            }
            catch (Exception) when (!lifetime.CanRollback)
            {
                // Loss or caller cancellation during rollback must not replace the original append failure.
                ExceptionDispatchInfo.Capture(appendFailure).Throw();
                throw;
            }

            throw;
        }

        // A commit exception may occur after the cursor advanced; rollback would then delete committed history.
        try
        {
            lifetime.ThrowIfFailed();
            await Repository.CommitCursorPositionAsync(
                brookId,
                finalPosition,
                lifetime.ThrowIfFailed,
                cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.CursorCommitFailed(exception, brookId, finalPosition);
            throw;
        }

        LogLargeBatchCommitted(Logger, brookId, finalPosition, batches.Count, null);
        return new(finalPosition);
    }

    private async Task<BrookPosition> AppendSingleBatchAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition currentCursor,
        long finalPosition,
        AppendLeaseLifetime lifetime,
        CancellationToken cancellationToken
    )
    {
        lifetime.ThrowIfFailed();
        long estBatchSize = SizeEstimator.EstimateBatchSize(events);
        LogSingleBatchStart(Logger, brookId, events.Count, estBatchSize, currentCursor.Value + 1, finalPosition, null);
        List<EventStorageModel> storageEvents = events.Select(EventMapper.Map).ToList();

        // Fallback to non-transactional flow (pending cursor -> append -> commit) to ensure reliability with emulator
        await Repository.CreatePendingCursorAsync(
            brookId,
            currentCursor,
            finalPosition,
            lifetime.ThrowIfFailed,
            cancellationToken);
        lifetime.ThrowIfFailed();
        await RetryPolicy.ExecuteAsync(
            async () =>
            {
                lifetime.ThrowIfFailed();
                await Repository.AppendEventBatchAsync(
                    brookId,
                    storageEvents,
                    currentCursor.Value + 1,
                    lifetime.ThrowIfFailed,
                    cancellationToken);
                return true;
            },
            cancellationToken);
        lifetime.ThrowIfFailed();
        await Repository.CommitCursorPositionAsync(brookId, finalPosition, lifetime.ThrowIfFailed, cancellationToken);
        LogSingleBatchCommitted(Logger, brookId, finalPosition, 200, 0, null);
        return new(finalPosition);
    }

    /// <summary>
    ///     Removes the appended events and pending cursor while ownership remains healthy.
    /// </summary>
    /// <param name="brookId">The brook whose partial append is being rolled back.</param>
    /// <param name="originalCursor">The position before the append began.</param>
    /// <param name="failedFinalPosition">The final successfully appended position to remove.</param>
    /// <param name="lifetime">The append's lease owner and storage dispatch guard.</param>
    /// <param name="cancellationToken">Cancellation shared by the append's storage operations.</param>
    /// <returns>A task representing rollback and verification of the deleted events.</returns>
    private async Task RollbackLargeBatchAsync(
        BrookKey brookId,
        BrookPosition originalCursor,
        long failedFinalPosition,
        AppendLeaseLifetime lifetime,
        CancellationToken cancellationToken
    )
    {
        List<Exception> rollbackErrors = new();
        List<long> remainingEvents = new();

        // Helper: attempt action with retry policy and record a friendly error on failure
        async Task TryWithRetryAsync(
            Func<Task> action,
            string errorMessage
        )
        {
            try
            {
                await RetryPolicy.ExecuteAsync(
                    async () =>
                    {
                        lifetime.ThrowIfFailed();
                        await action();
                        return true;
                    },
                    cancellationToken);
            }
            catch (Exception ex) when (lifetime.CanRollback && ShouldRecordRollbackFailure(ex))
            {
                rollbackErrors.Add(new InvalidOperationException(errorMessage, ex));
            }
        }

        // First pass: attempt to delete all appended events
        for (long pos = originalCursor.Value + 1; pos <= failedFinalPosition; pos++)
        {
            long capturedPos = pos; // avoid modified closure
            await TryWithRetryAsync(
                () => Repository.DeleteEventAsync(brookId, capturedPos, lifetime.ThrowIfFailed, cancellationToken),
                $"Failed to delete event at position {capturedPos}");
        }

        // Delete pending cursor state
        await TryWithRetryAsync(
            () => Repository.DeletePendingCursorAsync(brookId, lifetime.ThrowIfFailed, cancellationToken),
            "Failed to delete pending cursor");

        // Second pass: verify all events are actually deleted
        for (long pos = originalCursor.Value + 1; pos <= failedFinalPosition; pos++)
        {
            try
            {
                lifetime.ThrowIfFailed();
                bool eventExists = await Repository.EventExistsAsync(brookId, pos, cancellationToken);
                if (eventExists)
                {
                    remainingEvents.Add(pos);
                }
            }
            catch (Exception ex) when (lifetime.CanRollback && ShouldRecordRollbackFailure(ex))
            {
                rollbackErrors.Add(
                    new InvalidOperationException($"Failed to verify deletion of event at position {pos}", ex));
            }
        }

        // If there are any issues, throw an aggregate exception
        if ((rollbackErrors.Count > 0) || (remainingEvents.Count > 0))
        {
            List<Exception> allErrors = new(rollbackErrors);
            if (remainingEvents.Count > 0)
            {
                allErrors.Add(
                    new InvalidOperationException(
                        $"Rollback incomplete: {remainingEvents.Count} events still exist at positions: {string.Join(", ", remainingEvents)}"));
            }

            LogRollbackFailed(
                Logger,
                brookId,
                originalCursor.Value,
                failedFinalPosition,
                string.Join(", ", remainingEvents),
                new AggregateException(allErrors));
            throw new AggregateException("Rollback failed - brook may be in an inconsistent state", allErrors);
        }

        LogRollbackSucceeded(Logger, brookId, originalCursor.Value, null);
    }
}