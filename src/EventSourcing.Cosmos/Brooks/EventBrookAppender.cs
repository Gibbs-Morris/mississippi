using Microsoft.Azure.Cosmos;
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
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventBrookAppender" /> class.
    /// </summary>
    /// <param name="repository">The Cosmos repository for low-level operations.</param>
    /// <param name="lockManager">The distributed lock manager for concurrency control.</param>
    /// <param name="sizeEstimator">The batch size estimator for optimizing batch operations.</param>
    /// <param name="retryPolicy">The retry policy for handling transient failures.</param>
    /// <param name="options">The configuration options for brook storage.</param>
    /// <param name="eventMapper">The mapper for converting events to storage models.</param>
    /// <param name="recoveryService">The brook recovery service for head position management.</param>
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

    private ICosmosRepository Repository { get; }

    private IDistributedLockManager LockManager { get; }

    private IBatchSizeEstimator SizeEstimator { get; }

    private IRetryPolicy RetryPolicy { get; }

    private BrookStorageOptions Options { get; }

    private IMapper<BrookEvent, EventStorageModel> EventMapper { get; }

    private IBrookRecoveryService RecoveryService { get; }

    private ILogger<EventBrookAppender> Logger { get; }

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
        // Get current head position while holding the lock to ensure consistency
        BrookPosition currentHead = await RecoveryService.GetOrRecoverHeadPositionAsync(brookId, cancellationToken);

        // Perform optimistic concurrency check inside the lock
        if (expectedVersion.HasValue && (expectedVersion.Value != currentHead))
        {
            throw new OptimisticConcurrencyException(
                $"Expected version {expectedVersion.Value} but current head is {currentHead}");
        }

        // Check for potential overflow in position calculation
        if (currentHead.Value > (long.MaxValue - events.Count))
        {
            throw new InvalidOperationException(
                $"Position overflow: current head {currentHead.Value} + {events.Count} events would exceed maximum position");
        }

        long finalPosition = currentHead.Value + events.Count;
        long estimatedSize = SizeEstimator.EstimateBatchSize(events);
        Logger.LogInformation(
            "CosmosAppender: brook={Brook} count={Count} estSize={Size}B head={Head} -> final={Final}",
            brookId,
            events.Count,
            estimatedSize,
            currentHead.Value,
            finalPosition);
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
        Logger.LogInformation(
            "CosmosAppender: SingleBatch brook={Brook} count={Count} estSize={Size}B startPos={Start} final={Final}",
            brookId,
            events.Count,
            estBatchSize,
            currentHead.Value + 1,
            finalPosition);
        List<EventStorageModel> storageEvents = events.Select(EventMapper.Map).ToList();
        TransactionalBatchResponse response = await RetryPolicy.ExecuteAsync(
            async () => await Repository.ExecuteTransactionalBatchAsync(
                brookId,
                storageEvents,
                currentHead,
                finalPosition,
                cancellationToken),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to append events: {response.ErrorMessage}");
        }

        Logger.LogInformation(
            "CosmosAppender: SingleBatch committed brook={Brook} newHead={Head} status={Status} charge={Charge}",
            brookId,
            finalPosition,
            (int)response.StatusCode,
            response.RequestCharge);
        return new(finalPosition);
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
            Logger.LogInformation(
                "CosmosAppender: LargeBatch brook={Brook} totalCount={Total} batches={Batches} head={Head} final={Final} maxPerBatch={MaxEv} maxReq={MaxReq}",
                brookId,
                events.Count,
                batches.Count,
                currentHead.Value,
                finalPosition,
                Options.MaxEventsPerBatch,
                Options.MaxRequestSizeBytes);
            DateTimeOffset lastRenewal = DateTimeOffset.UtcNow;
            for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
            {
                // Renew the lease periodically based on threshold or every 5 batches
                if ((batchIndex > 0) && (((batchIndex % 5) == 0) ||
                                          (DateTimeOffset.UtcNow - lastRenewal).TotalSeconds >= Options.LeaseRenewalThresholdSeconds))
                {
                    await distributedLock.RenewAsync(cancellationToken);
                    lastRenewal = DateTimeOffset.UtcNow;
                }

                IReadOnlyList<BrookEvent> batchEvents = batches[batchIndex];
                long batchStartPosition = currentHead.Value + processedEvents + 1;
                long estBatchSize = SizeEstimator.EstimateBatchSize(batchEvents);
                Logger.LogInformation(
                    "CosmosAppender: Batch {BatchIdx}/{BatchCount} brook={Brook} count={Count} estSize={Size}B startPos={Start}",
                    batchIndex + 1,
                    batches.Count,
                    brookId,
                    batchEvents.Count,
                    estBatchSize,
                    batchStartPosition);
                List<EventStorageModel> storageBatchEvents = batchEvents.Select(EventMapper.Map).ToList();
                await Repository.AppendEventBatchAsync(
                    brookId,
                    storageBatchEvents,
                    batchStartPosition,
                    cancellationToken);
                processedEvents += batchEvents.Count;
            }

            await Repository.CommitHeadPositionAsync(brookId, finalPosition, cancellationToken);
            Logger.LogInformation(
                "CosmosAppender: LargeBatch committed brook={Brook} newHead={Head} batches={Batches}",
                brookId,
                finalPosition,
                batches.Count);
            return new(finalPosition);
        }
        catch
        {
            // Rollback only what we actually appended
            long lastSuccessfulPosition = currentHead.Value; // default to head if none succeeded
            // processedEvents reflects successfully created items
            await RollbackLargeBatchAsync(brookId, new BrookPosition(currentHead.Value), currentHead.Value + processedEvents, cancellationToken);
            throw;
        }
    }

    private async Task RollbackLargeBatchAsync(
        BrookKey brookId,
        BrookPosition originalHead,
        long failedFinalPosition,
        CancellationToken cancellationToken
    )
    {
        List<Exception> rollbackErrors = new();

        // First pass: attempt to delete all events
        for (long pos = originalHead.Value + 1; pos <= failedFinalPosition; pos++)
        {
            try
            {
                await RetryPolicy.ExecuteAsync(
                    async () =>
                    {
                        await Repository.DeleteEventAsync(brookId, pos, cancellationToken);
                        return true;
                    },
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                rollbackErrors.Add(new InvalidOperationException($"Failed to delete event at position {pos}", ex));
            }
            catch (TimeoutException ex)
            {
                rollbackErrors.Add(new InvalidOperationException($"Failed to delete event at position {pos}", ex));
            }
            catch (HttpRequestException ex)
            {
                rollbackErrors.Add(new InvalidOperationException($"Failed to delete event at position {pos}", ex));
            }
        }

        // Delete pending head
        try
        {
            await RetryPolicy.ExecuteAsync(
                async () =>
                {
                    await Repository.DeletePendingHeadAsync(brookId, cancellationToken);
                    return true;
                },
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            rollbackErrors.Add(new InvalidOperationException("Failed to delete pending head", ex));
        }
        catch (TimeoutException ex)
        {
            rollbackErrors.Add(new InvalidOperationException("Failed to delete pending head", ex));
        }
        catch (HttpRequestException ex)
        {
            rollbackErrors.Add(new InvalidOperationException("Failed to delete pending head", ex));
        }

        // Second pass: verify all events are actually deleted
        List<long> remainingEvents = new();
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
            catch (InvalidOperationException ex)
            {
                rollbackErrors.Add(
                    new InvalidOperationException($"Failed to verify deletion of event at position {pos}", ex));
            }
            catch (TimeoutException ex)
            {
                rollbackErrors.Add(
                    new InvalidOperationException($"Failed to verify deletion of event at position {pos}", ex));
            }
            catch (HttpRequestException ex)
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

            Logger.LogError(
                new AggregateException(allErrors),
                "CosmosAppender: Rollback failed brook={Brook} originalHead={Head} failedFinal={Final} remainingEvents={Remaining}",
                brookId,
                originalHead.Value,
                failedFinalPosition,
                string.Join(", ", remainingEvents));
            throw new AggregateException("Rollback failed - brook may be in an inconsistent state", allErrors);
        }
        else
        {
            Logger.LogWarning(
                "CosmosAppender: Rollback succeeded brook={Brook} restoredHead={Head}",
                brookId,
                originalHead.Value);
        }
    }
}