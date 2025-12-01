using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Batching;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;


namespace Mississippi.EventSourcing.Cosmos.Brooks;

/// <summary>
///     Cosmos DB implementation of the event brook appender for writing events to brooks.
/// </summary>
internal class EventBrookAppender : IEventBrookAppender
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
    ///     Initializes a new instance of the <see cref="EventBrookAppender" /> class.
    /// </summary>
    /// <param name="repository">The Cosmos repository for low-level operations.</param>
    /// <param name="lockManager">The distributed lock manager for concurrency control.</param>
    /// <param name="sizeEstimator">The batch size estimator for optimizing batch operations.</param>
    /// <param name="retryPolicy">The retry policy for handling transient failures.</param>
    /// <param name="options">The configuration options for brook storage.</param>
    /// <param name="eventMapper">The mapper for converting events to storage models.</param>
    /// <param name="recoveryService">The brook recovery service for cursor position management.</param>
    /// <param name="logger">The logger used to record operational diagnostics.</param>
    public EventBrookAppender(
        ICosmosRepository repository,
        IDistributedLockManager lockManager,
        IBatchSizeEstimator sizeEstimator,
        IRetryPolicy retryPolicy,
        IOptions<BrookStorageOptions> options,
        IMapper<BrookEvent, EventStorageModel> eventMapper,
        IBrookRecoveryService recoveryService,
        ILogger<EventBrookAppender> logger
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
    }

    private IMapper<BrookEvent, EventStorageModel> EventMapper { get; }

    private IDistributedLockManager LockManager { get; }

    private ILogger<EventBrookAppender> Logger { get; }

    private BrookStorageOptions Options { get; }

    private IBrookRecoveryService RecoveryService { get; }

    private ICosmosRepository Repository { get; }

    private IRetryPolicy RetryPolicy { get; }

    private IBatchSizeEstimator SizeEstimator { get; }

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

        await using IDistributedLock distributedLock = await LockManager.AcquireLockAsync(
            brookId.ToString(),
            TimeSpan.FromSeconds(Options.LeaseDurationSeconds),
            cancellationToken);
        return await AppendEventsWithLockAsync(brookId, events, expectedVersion, distributedLock, cancellationToken);
    }

    private async Task<BrookPosition> AppendEventsWithLockAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion,
        IDistributedLock distributedLock,
        CancellationToken cancellationToken
    )
    {
        // Get current cursor position while holding the lock to ensure consistency
        BrookPosition currentHead = await RecoveryService.GetOrRecoverCursorPositionAsync(brookId, cancellationToken);

        // Perform optimistic concurrency check inside the lock
        if (expectedVersion.HasValue && (expectedVersion.Value != currentHead))
        {
            throw new OptimisticConcurrencyException(
                $"Expected version {expectedVersion.Value} but current cursor is {currentHead}");
        }

        // Check for potential overflow in position calculation
        if (currentHead.Value > (long.MaxValue - events.Count))
        {
            throw new InvalidOperationException(
                $"Position overflow: current cursor {currentHead.Value} + {events.Count} events would exceed maximum position");
        }

        long finalPosition = currentHead.Value + events.Count;
        long estimatedSize = SizeEstimator.EstimateBatchSize(events);
        LogAppenderSummary(Logger, brookId, events.Count, estimatedSize, currentHead.Value, finalPosition, null);
        if ((events.Count > Options.MaxEventsPerBatch) || (estimatedSize > Options.MaxRequestSizeBytes))
        {
            return await AppendLargeBatchAsync(
                brookId,
                events,
                currentHead,
                finalPosition,
                distributedLock,
                cancellationToken);
        }

        return await AppendSingleBatchAsync(
            brookId,
            events,
            currentHead,
            finalPosition,
            distributedLock,
            cancellationToken);
    }

    private async Task<BrookPosition> AppendLargeBatchAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition currentHead,
        long finalPosition,
        IDistributedLock distributedLock,
        CancellationToken cancellationToken
    )
    {
        // Build batches first while holding the lock, so we fail fast for oversize events
        List<IReadOnlyList<BrookEvent>> batches = SizeEstimator.CreateSizeLimitedBatches(
                events,
                Options.MaxEventsPerBatch,
                Options.MaxRequestSizeBytes)
            .ToList();
        await Repository.CreatePendingHeadAsync(brookId, currentHead, finalPosition, cancellationToken);
        int processedEvents = 0;
        try
        {
            LogLargeBatchSummaryPart1(Logger, brookId, events.Count, batches.Count, null);
            LogLargeBatchSummaryPart2(
                Logger,
                currentHead.Value,
                finalPosition,
                Options.MaxEventsPerBatch,
                Options.MaxRequestSizeBytes,
                null);
            DateTimeOffset lastRenewal = DateTimeOffset.UtcNow;
            for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
            {
                // Renew the lease periodically based on threshold or every 5 batches
                if ((batchIndex > 0) &&
                    (((batchIndex % 5) == 0) ||
                     ((DateTimeOffset.UtcNow - lastRenewal).TotalSeconds >= Options.LeaseRenewalThresholdSeconds)))
                {
                    await distributedLock.RenewAsync(cancellationToken);
                    lastRenewal = DateTimeOffset.UtcNow;
                }

                IReadOnlyList<BrookEvent> batchEvents = batches[batchIndex];
                long batchStartPosition = currentHead.Value + processedEvents + 1;
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
                    cancellationToken);
                processedEvents += batchEvents.Count;
            }

            await Repository.CommitHeadPositionAsync(brookId, finalPosition, cancellationToken);
            LogLargeBatchCommitted(Logger, brookId, finalPosition, batches.Count, null);
            return new(finalPosition);
        }
        catch
        {
            // Rollback only what we actually appended
            // processedEvents reflects successfully created items
            await RollbackLargeBatchAsync(
                brookId,
                new(currentHead.Value),
                currentHead.Value + processedEvents,
                cancellationToken);
            throw;
        }
    }

    private async Task<BrookPosition> AppendSingleBatchAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition currentHead,
        long finalPosition,
        IDistributedLock distributedLock,
        CancellationToken cancellationToken
    )
    {
        await distributedLock.RenewAsync(cancellationToken);
        long estBatchSize = SizeEstimator.EstimateBatchSize(events);
        LogSingleBatchStart(Logger, brookId, events.Count, estBatchSize, currentHead.Value + 1, finalPosition, null);
        List<EventStorageModel> storageEvents = events.Select(EventMapper.Map).ToList();

        // Fallback to non-transactional flow (pending head -> append -> commit) to ensure reliability with emulator
        await Repository.CreatePendingHeadAsync(brookId, currentHead, finalPosition, cancellationToken);
        await RetryPolicy.ExecuteAsync(
            async () =>
            {
                await Repository.AppendEventBatchAsync(
                    brookId,
                    storageEvents,
                    currentHead.Value + 1,
                    cancellationToken);
                return true;
            },
            cancellationToken);
        await Repository.CommitHeadPositionAsync(brookId, finalPosition, cancellationToken);
        LogSingleBatchCommitted(Logger, brookId, finalPosition, 200, 0, null);
        return new(finalPosition);
    }

    private async Task RollbackLargeBatchAsync(
        BrookKey brookId,
        BrookPosition originalHead,
        long failedFinalPosition,
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
                        await action();
                        return true;
                    },
                    cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidOperationException ||
                                       ex is TimeoutException ||
                                       ex is HttpRequestException)
            {
                rollbackErrors.Add(new InvalidOperationException(errorMessage, ex));
            }
        }

        // First pass: attempt to delete all appended events
        for (long pos = originalHead.Value + 1; pos <= failedFinalPosition; pos++)
        {
            long capturedPos = pos; // avoid modified closure
            await TryWithRetryAsync(
                () => Repository.DeleteEventAsync(brookId, capturedPos, cancellationToken),
                $"Failed to delete event at position {capturedPos}");
        }

        // Delete pending head
        await TryWithRetryAsync(
            () => Repository.DeletePendingHeadAsync(brookId, cancellationToken),
            "Failed to delete pending head");

        // Second pass: verify all events are actually deleted
        for (long pos = originalHead.Value + 1; pos <= failedFinalPosition; pos++)
        {
            try
            {
                bool eventExists = await Repository.EventExistsAsync(brookId, pos, cancellationToken);
                if (eventExists)
                {
                    remainingEvents.Add(pos);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException ||
                                       ex is TimeoutException ||
                                       ex is HttpRequestException)
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
                originalHead.Value,
                failedFinalPosition,
                string.Join(", ", remainingEvents),
                new AggregateException(allErrors));
            throw new AggregateException("Rollback failed - brook may be in an inconsistent state", allErrors);
        }

        LogRollbackSucceeded(Logger, brookId, originalHead.Value, null);
    }
}
