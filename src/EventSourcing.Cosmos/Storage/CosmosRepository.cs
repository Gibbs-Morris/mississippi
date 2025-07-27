using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Retry;

namespace Mississippi.EventSourcing.Cosmos.Storage;

internal class CosmosRepository : ICosmosRepository
{
    private Container Container { get; }
    private IRetryPolicy RetryPolicy { get; }
    private IMapper<HeadDocument, HeadStorageModel> HeadDocumentMapper { get; }
    private IMapper<EventDocument, EventStorageModel> EventDocumentMapper { get; }

    public CosmosRepository(
        Container container,
        IRetryPolicy retryPolicy,
        IMapper<HeadDocument, HeadStorageModel> headDocumentMapper,
        IMapper<EventDocument, EventStorageModel> eventDocumentMapper)
    {
        Container = container;
        RetryPolicy = retryPolicy;
        HeadDocumentMapper = headDocumentMapper;
        EventDocumentMapper = eventDocumentMapper;
    }

    public async Task<HeadStorageModel?> GetHeadDocumentAsync(BrookKey brookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await RetryPolicy.ExecuteAsync(async () =>
                await Container.ReadItemAsync<HeadDocument>("head", new PartitionKey(brookId.ToString()),
                    cancellationToken: cancellationToken));
            return HeadDocumentMapper.Map(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<HeadStorageModel?> GetPendingHeadDocumentAsync(BrookKey brookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await RetryPolicy.ExecuteAsync(async () =>
                await Container.ReadItemAsync<HeadDocument>("head-pending", new PartitionKey(brookId.ToString()),
                    cancellationToken: cancellationToken));
            return HeadDocumentMapper.Map(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task CreatePendingHeadAsync(BrookKey brookId, BrookPosition currentHead, long finalPosition, CancellationToken cancellationToken = default)
    {
        var pendingHeadDoc = new HeadDocument
        {
            Id = "head-pending",
            Type = "head-pending",
            Position = new BrookPosition(finalPosition),
            OriginalPosition = currentHead
        };

        var partitionKey = new PartitionKey(brookId.ToString());
        await RetryPolicy.ExecuteAsync(async () =>
            await Container.CreateItemAsync(pendingHeadDoc, partitionKey, cancellationToken: cancellationToken));
    }

    public async Task CommitHeadPositionAsync(BrookKey brookId, long finalPosition, CancellationToken cancellationToken = default)
    {
        var partitionKey = new PartitionKey(brookId.ToString());
        var batch = Container.CreateTransactionalBatch(partitionKey);

        var headDoc = new HeadDocument
        {
            Id = "head",
            Type = "head",
            Position = new BrookPosition(finalPosition)
        };
        batch.UpsertItem(headDoc);
        batch.DeleteItem("head-pending");

        var response = await RetryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync(cancellationToken));

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to commit head position: {response.ErrorMessage}");
        }
    }

    public async Task<bool> EventExistsAsync(BrookKey brookId, long position, CancellationToken cancellationToken = default)
    {
        try
        {
            await Container.ReadItemAsync<EventDocument>(position.ToString(), new PartitionKey(brookId.ToString()),
                cancellationToken: cancellationToken);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<ISet<long>> GetExistingEventPositionsAsync(BrookKey brookId, long startPosition, long endPosition, CancellationToken cancellationToken = default)
    {
        var partitionKey = new PartitionKey(brookId.ToString());
        var query = new QueryDefinition(
                "SELECT VALUE c.position FROM c WHERE c.type = 'event' AND c.position >= @start AND c.position <= @end")
            .WithParameter("@start", startPosition)
            .WithParameter("@end", endPosition);

        var iterator = Container.GetItemQueryIterator<long>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = partitionKey
        });

        var existingPositions = new HashSet<long>();
        while (iterator.HasMoreResults)
        {
            var response = await RetryPolicy.ExecuteAsync(async () => await iterator.ReadNextAsync(cancellationToken));
            foreach (var position in response)
            {
                existingPositions.Add(position);
            }
        }

        return existingPositions;
    }

    public async Task DeleteEventAsync(BrookKey brookId, long position, CancellationToken cancellationToken = default)
    {
        try
        {
            await Container.DeleteItemAsync<EventDocument>(position.ToString(), new PartitionKey(brookId.ToString()),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    public async Task DeletePendingHeadAsync(BrookKey brookId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Container.DeleteItemAsync<HeadDocument>("head-pending", new PartitionKey(brookId.ToString()),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    public async Task AppendEventBatchAsync(BrookKey brookId, IReadOnlyList<EventStorageModel> events, long startPosition, CancellationToken cancellationToken = default)
    {
        var partitionKey = new PartitionKey(brookId.ToString());
        var batch = Container.CreateTransactionalBatch(partitionKey);

        var position = startPosition;
        foreach (var eventStorageModel in events)
        {
            var eventDoc = new EventDocument
            {
                Id = position.ToString(),
                Type = "event",
                Position = position,
                EventId = eventStorageModel.EventId,
                Source = eventStorageModel.Source,
                EventType = eventStorageModel.EventType,
                DataContentType = eventStorageModel.DataContentType,
                Data = eventStorageModel.Data,
                Time = eventStorageModel.Time
            };
            batch.CreateItem(eventDoc);
            position++;
        }

        var response = await RetryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync(cancellationToken));

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to append event batch: {response.ErrorMessage}");
        }
    }

    public async Task<TransactionalBatchResponse> ExecuteTransactionalBatchAsync(BrookKey brookId, IReadOnlyList<EventStorageModel> events, BrookPosition currentHead, long newPosition, CancellationToken cancellationToken = default)
    {
        var partitionKey = new PartitionKey(brookId.ToString());
        var batch = Container.CreateTransactionalBatch(partitionKey);

        var headDoc = new HeadDocument
        {
            Id = "head",
            Type = "head",
            Position = new BrookPosition(newPosition)
        };

        if (currentHead.NotSet)
        {
            batch.CreateItem(headDoc);
        }
        else
        {
            batch.ReplaceItem("head", headDoc);
        }

        var position = currentHead.Value + 1;
        foreach (var eventStorageModel in events)
        {
            var eventDoc = new EventDocument
            {
                Id = position.ToString(),
                Type = "event",
                Position = position,
                EventId = eventStorageModel.EventId,
                Source = eventStorageModel.Source,
                EventType = eventStorageModel.EventType,
                DataContentType = eventStorageModel.DataContentType,
                Data = eventStorageModel.Data,
                Time = eventStorageModel.Time
            };
            batch.CreateItem(eventDoc);
            position++;
        }

        return await batch.ExecuteAsync(cancellationToken);
    }

    public async IAsyncEnumerable<EventStorageModel> QueryEventsAsync(BrookRangeKey brookRange, int batchSize, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var partitionKey = new PartitionKey(brookRange.ToBrookCompositeKey().ToString());
        var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = 'event' AND c.position >= @start AND c.position <= @end ORDER BY c.position")
            .WithParameter("@start", brookRange.Start.Value)
            .WithParameter("@end", brookRange.End.Value);

        var iterator = Container.GetItemQueryIterator<EventDocument>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = partitionKey,
            MaxItemCount = batchSize
        });

        while (iterator.HasMoreResults)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await RetryPolicy.ExecuteAsync(async () => await iterator.ReadNextAsync(cancellationToken));

            foreach (var eventDoc in response)
            {
                yield return EventDocumentMapper.Map(eventDoc);
            }
        }
    }
}