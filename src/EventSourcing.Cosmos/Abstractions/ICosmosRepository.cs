using Microsoft.Azure.Cosmos;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Abstractions;

internal interface ICosmosRepository
{
    Task<HeadStorageModel?> GetHeadDocumentAsync(BrookKey brookId, CancellationToken cancellationToken = default);
    Task<HeadStorageModel?> GetPendingHeadDocumentAsync(BrookKey brookId, CancellationToken cancellationToken = default);
    Task CreatePendingHeadAsync(BrookKey brookId, BrookPosition currentHead, long finalPosition, CancellationToken cancellationToken = default);
    Task CommitHeadPositionAsync(BrookKey brookId, long finalPosition, CancellationToken cancellationToken = default);
    Task<bool> EventExistsAsync(BrookKey brookId, long position, CancellationToken cancellationToken = default);
    Task<ISet<long>> GetExistingEventPositionsAsync(BrookKey brookId, long startPosition, long endPosition, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(BrookKey brookId, long position, CancellationToken cancellationToken = default);
    Task DeletePendingHeadAsync(BrookKey brookId, CancellationToken cancellationToken = default);
    Task AppendEventBatchAsync(BrookKey brookId, IReadOnlyList<EventStorageModel> events, long startPosition, CancellationToken cancellationToken = default);
    Task<TransactionalBatchResponse> ExecuteTransactionalBatchAsync(BrookKey brookId, IReadOnlyList<EventStorageModel> events, BrookPosition currentHead, long newPosition, CancellationToken cancellationToken = default);
    IAsyncEnumerable<EventStorageModel> QueryEventsAsync(BrookRangeKey brookRange, int batchSize, CancellationToken cancellationToken = default);
}