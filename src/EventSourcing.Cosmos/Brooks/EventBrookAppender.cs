using Microsoft.Extensions.Options;
using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Batching;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Brooks;

/// <summary>
/// Cosmos DB implementation of the event brook appender for writing events to brooks.
/// </summary>
internal class EventBrookAppender : IEventBrookAppender
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventBrookAppender"/> class.
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
        IBrookRecoveryService recoveryService)
    {
        Repository = repository;
        LockManager = lockManager;
        SizeEstimator = sizeEstimator;
        RetryPolicy = retryPolicy;
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        EventMapper = eventMapper;
        RecoveryService = recoveryService;
    }

    private ICosmosRepository Repository { get; }

    private IDistributedLockManager LockManager { get; }

    private IBatchSizeEstimator SizeEstimator { get; }

    private IRetryPolicy RetryPolicy { get; }

    private BrookStorageOptions Options { get; }

    private IMapper<BrookEvent, EventStorageModel> EventMapper { get; }

    private IBrookRecoveryService RecoveryService { get; }

    /// <summary>
    /// Appends a collection of events to the specified brook stream.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target stream.</param>
    /// <param name="events">The collection of events to append to the stream.</param>
    /// <param name="expectedVersion">The expected version for optimistic concurrency control.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The position after successfully appending all events.</returns>
    public async Task<BrookPosition> AppendEventsAsync(BrookKey brookId, IReadOnlyList<BrookEvent> events, BrookPosition? expectedVersion, CancellationToken cancellationToken = default)
    {
        await using var distributedLock = await LockManager.AcquireLockAsync(brookId.ToString(), TimeSpan.FromSeconds(Options.LeaseDurationSeconds), cancellationToken);

        return await AppendEventsWithLockAsync(brookId, events, expectedVersion, distributedLock, cancellationToken);
    }

    private async Task<BrookPosition> AppendEventsWithLockAsync(BrookKey brookId, IReadOnlyList<BrookEvent> events, BrookPosition? expectedVersion, IDistributedLock distributedLock, CancellationToken cancellationToken)
    {
        var currentHead = await RecoveryService.GetOrRecoverHeadPositionAsync(brookId, cancellationToken);

        if (expectedVersion.HasValue && expectedVersion.Value != currentHead)
        {
            throw new OptimisticConcurrencyException(
                $"Expected version {expectedVersion.Value} but current head is {currentHead}");
        }

        var finalPosition = currentHead.Value + events.Count;
        var estimatedSize = SizeEstimator.EstimateBatchSize(events);

        if (events.Count > Options.MaxEventsPerBatch || estimatedSize > Options.MaxRequestSizeBytes)
        {
            return await AppendLargeBatchAsync(brookId, events, currentHead, finalPosition, distributedLock, cancellationToken);
        }
        else
        {
            return await AppendSingleBatchAsync(brookId, events, currentHead, finalPosition, distributedLock, cancellationToken);
        }
    }

    private async Task<BrookPosition> AppendSingleBatchAsync(BrookKey brookId, IReadOnlyList<BrookEvent> events, BrookPosition currentHead, long finalPosition, IDistributedLock distributedLock, CancellationToken cancellationToken)
    {
        await distributedLock.RenewAsync(cancellationToken);

        var storageEvents = events.Select(EventMapper.Map).ToList();
        var response = await RetryPolicy.ExecuteAsync(
            async () =>
            await Repository.ExecuteTransactionalBatchAsync(
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

        return new BrookPosition(finalPosition);
    }

    private async Task<BrookPosition> AppendLargeBatchAsync(BrookKey brookId, IReadOnlyList<BrookEvent> events, BrookPosition currentHead, long finalPosition, IDistributedLock distributedLock, CancellationToken cancellationToken)
    {
        await Repository.CreatePendingHeadAsync(brookId, currentHead, finalPosition, cancellationToken);

        try
        {
            var batches = SizeEstimator.CreateSizeLimitedBatches(events, Options.MaxEventsPerBatch, Options.MaxRequestSizeBytes).ToList();
            var processedEvents = 0;

            for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
            {
                if (batchIndex > 0 && batchIndex % 5 == 0)
                {
                    await distributedLock.RenewAsync(cancellationToken);
                }

                var batchEvents = batches[batchIndex];
                var batchStartPosition = currentHead.Value + processedEvents + 1;
                var storageBatchEvents = batchEvents.Select(EventMapper.Map).ToList();

                await Repository.AppendEventBatchAsync(brookId, storageBatchEvents, batchStartPosition, cancellationToken);
                processedEvents += batchEvents.Count;
            }

            await Repository.CommitHeadPositionAsync(brookId, finalPosition, cancellationToken);
            return new BrookPosition(finalPosition);
        }
        catch
        {
            await RollbackLargeBatchAsync(brookId, currentHead, finalPosition, cancellationToken);
            throw;
        }
    }

    private async Task RollbackLargeBatchAsync(BrookKey brookId, BrookPosition originalHead, long failedFinalPosition, CancellationToken cancellationToken)
    {
        for (long pos = originalHead.Value + 1; pos <= failedFinalPosition; pos++)
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