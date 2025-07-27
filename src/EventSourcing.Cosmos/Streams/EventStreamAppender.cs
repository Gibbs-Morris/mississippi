using Microsoft.Extensions.Options;
using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Batching;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Streams;

internal class EventStreamAppender : IEventStreamAppender
{
    private ICosmosRepository Repository { get; }
    private IDistributedLockManager LockManager { get; }
    private IBatchSizeEstimator SizeEstimator { get; }
    private IRetryPolicy RetryPolicy { get; }
    private BrookStorageOptions Options { get; }
    private IMapper<BrookEvent, EventStorageModel> EventMapper { get; }
    private IStreamRecoveryService RecoveryService { get; }

    public EventStreamAppender(
        ICosmosRepository repository,
        IDistributedLockManager lockManager,
        IBatchSizeEstimator sizeEstimator,
        IRetryPolicy retryPolicy,
        IOptions<BrookStorageOptions> options,
        IMapper<BrookEvent, EventStorageModel> eventMapper,
        IStreamRecoveryService recoveryService)
    {
        Repository = repository;
        LockManager = lockManager;
        SizeEstimator = sizeEstimator;
        RetryPolicy = retryPolicy;
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        EventMapper = eventMapper;
        RecoveryService = recoveryService;
    }

    public async Task<BrookPosition> AppendEventsAsync(BrookKey brookId, IReadOnlyList<BrookEvent> events, BrookPosition? expectedVersion, CancellationToken cancellationToken)
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
        var response = await RetryPolicy.ExecuteAsync(async () =>
            await Repository.ExecuteTransactionalBatchAsync(brookId, storageEvents, currentHead, finalPosition, cancellationToken));

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
            await RetryPolicy.ExecuteAsync(async () =>
            {
                await Repository.DeleteEventAsync(brookId, pos, cancellationToken);
                return true;
            });
        }
        await RetryPolicy.ExecuteAsync(async () =>
        {
            await Repository.DeletePendingHeadAsync(brookId, cancellationToken);
            return true;
        });
    }
}