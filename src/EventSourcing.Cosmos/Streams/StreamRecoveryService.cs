using Microsoft.Extensions.Options;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Streams;

internal class StreamRecoveryService : IStreamRecoveryService
{
    private ICosmosRepository Repository { get; }
    private IRetryPolicy RetryPolicy { get; }
    private IDistributedLockManager LockManager { get; }
    private BrookStorageOptions Options { get; }

    public StreamRecoveryService(ICosmosRepository repository, IRetryPolicy retryPolicy, IDistributedLockManager lockManager, IOptions<BrookStorageOptions> options)
    {
        Repository = repository;
        RetryPolicy = retryPolicy;
        LockManager = lockManager;
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<BrookPosition> GetOrRecoverHeadPositionAsync(BrookKey brookId, CancellationToken cancellationToken)
    {
        var headDocument = await RetryPolicy.ExecuteAsync(async () =>
            await Repository.GetHeadDocumentAsync(brookId, cancellationToken));

        if (headDocument == null)
        {
            var pendingHead = await RetryPolicy.ExecuteAsync(async () =>
                await Repository.GetPendingHeadDocumentAsync(brookId, cancellationToken));

            if (pendingHead != null)
            {
                await using var recoveryLock = await LockManager.AcquireLockAsync(
                    brookId.ToString(),
                    TimeSpan.FromSeconds(Options.LeaseDurationSeconds),
                    cancellationToken);

                await RecoverFromOrphanedOperationAsync(brookId, pendingHead, cancellationToken);
                headDocument = await RetryPolicy.ExecuteAsync(async () =>
                    await Repository.GetHeadDocumentAsync(brookId, cancellationToken));
            }
        }

        return headDocument?.Position ?? new BrookPosition(-1);
    }

    private async Task RecoverFromOrphanedOperationAsync(BrookKey brookId, HeadStorageModel pendingHead, CancellationToken cancellationToken)
    {
        var originalPosition = pendingHead.OriginalPosition?.Value ?? -1;
        var targetPosition = pendingHead.Position.Value;

        bool allEventsExist = await CheckAllEventsExistAsync(brookId, originalPosition, targetPosition, cancellationToken);

        if (allEventsExist)
        {
            await Repository.CommitHeadPositionAsync(brookId, targetPosition, cancellationToken);
        }
        else
        {
            await RollbackOrphanedOperationAsync(brookId, originalPosition, targetPosition, cancellationToken);
        }
    }

    private async Task<bool> CheckAllEventsExistAsync(BrookKey brookId, long originalPosition, long targetPosition, CancellationToken cancellationToken)
    {
        if (targetPosition - originalPosition > 10)
        {
            var existingPositions = await Repository.GetExistingEventPositionsAsync(brookId, originalPosition + 1, targetPosition, cancellationToken);
            var expectedCount = targetPosition - originalPosition;
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

    private async Task RollbackOrphanedOperationAsync(BrookKey brookId, long originalPosition, long targetPosition, CancellationToken cancellationToken)
    {
        for (long pos = originalPosition + 1; pos <= targetPosition; pos++)
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