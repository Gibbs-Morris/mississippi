using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;

using Microsoft.Azure.Cosmos;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Retry;


namespace Mississippi.EventSourcing.Cosmos.Storage;

/// <summary>
///     Repository implementation for Cosmos DB operations on brooks and events.
/// </summary>
internal class CosmosRepository : ICosmosRepository
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosRepository" /> class.
    /// </summary>
    /// <param name="container">The Cosmos DB container for storing data.</param>
    /// <param name="retryPolicy">The retry policy for handling transient failures.</param>
    /// <param name="headDocumentMapper">The mapper for head documents.</param>
    /// <param name="eventDocumentMapper">The mapper for event documents.</param>
    public CosmosRepository(
        Container container,
        IRetryPolicy retryPolicy,
        IMapper<HeadDocument, HeadStorageModel> headDocumentMapper,
        IMapper<EventDocument, EventStorageModel> eventDocumentMapper
    )
    {
        Container = container;
        RetryPolicy = retryPolicy;
        HeadDocumentMapper = headDocumentMapper;
        EventDocumentMapper = eventDocumentMapper;
    }

    private Container Container { get; }

    private IRetryPolicy RetryPolicy { get; }

    private IMapper<HeadDocument, HeadStorageModel> HeadDocumentMapper { get; }

    private IMapper<EventDocument, EventStorageModel> EventDocumentMapper { get; }

    /// <summary>
    ///     Gets the head document for a brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The head storage model, or null if not found.</returns>
    public async Task<HeadStorageModel?> GetHeadDocumentAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ItemResponse<HeadDocument>? response = await Container.ReadItemAsync<HeadDocument>(
                "head",
                new(brookId.ToString()),
                cancellationToken: cancellationToken);
            return HeadDocumentMapper.Map(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    ///     Gets the pending head document for a brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pending head storage model, or null if not found.</returns>
    public async Task<HeadStorageModel?> GetPendingHeadDocumentAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ItemResponse<HeadDocument>? response = await Container.ReadItemAsync<HeadDocument>(
                "head-pending",
                new(brookId.ToString()),
                cancellationToken: cancellationToken);
            return HeadDocumentMapper.Map(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    ///     Creates a pending head document for a brook transaction.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="currentHead">The current head position.</param>
    /// <param name="finalPosition">The final position to be committed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreatePendingHeadAsync(
        BrookKey brookId,
        BrookPosition currentHead,
        long finalPosition,
        CancellationToken cancellationToken = default
    )
    {
        HeadDocument pendingHeadDoc = new()
        {
            Id = "head-pending",
            Type = "head-pending",
            Position = finalPosition,
            OriginalPosition = currentHead.Value,
            BrookPartitionKey = brookId.ToString(),
        };
        PartitionKey partitionKey = new(brookId.ToString());
        await RetryPolicy.ExecuteAsync(
            async () => await Container.CreateItemAsync(
                pendingHeadDoc,
                partitionKey,
                cancellationToken: cancellationToken),
            cancellationToken);
    }

    /// <summary>
    ///     Commits the head position for a brook by finalizing the pending position.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="finalPosition">The final position to commit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CommitHeadPositionAsync(
        BrookKey brookId,
        long finalPosition,
        CancellationToken cancellationToken = default
    )
    {
        PartitionKey partitionKey = new(brookId.ToString());
        TransactionalBatch? batch = Container.CreateTransactionalBatch(partitionKey);
        HeadDocument headDoc = new()
        {
            Id = "head",
            Type = "head",
            Position = finalPosition,
            BrookPartitionKey = brookId.ToString(),
        };
        batch.UpsertItem(headDoc);
        batch.DeleteItem("head-pending");
        TransactionalBatchResponse? response = await RetryPolicy.ExecuteAsync(
            async () => await batch.ExecuteAsync(cancellationToken),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to commit head position: {response.ErrorMessage}");
        }
    }

    /// <summary>
    ///     Checks if an event exists at the specified position in a brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The position to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the event exists, false otherwise.</returns>
    public async Task<bool> EventExistsAsync(
        BrookKey brookId,
        long position,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await Container.ReadItemAsync<EventDocument>(
                position.ToString(CultureInfo.InvariantCulture),
                new(brookId.ToString()),
                cancellationToken: cancellationToken);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <summary>
    ///     Gets the positions of existing events within a specified range for a brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="startPosition">The start position of the range.</param>
    /// <param name="endPosition">The end position of the range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A set of positions where events exist.</returns>
    public async Task<ISet<long>> GetExistingEventPositionsAsync(
        BrookKey brookId,
        long startPosition,
        long endPosition,
        CancellationToken cancellationToken = default
    )
    {
        PartitionKey partitionKey = new(brookId.ToString());
        QueryDefinition? query = new QueryDefinition(
                "SELECT VALUE c.position FROM c WHERE c.type = 'event' AND c.position >= @start AND c.position <= @end")
            .WithParameter("@start", startPosition)
            .WithParameter("@end", endPosition);
        using FeedIterator<long>? iterator = Container.GetItemQueryIterator<long>(
            query,
            requestOptions: new()
            {
                PartitionKey = partitionKey,
            });
        HashSet<long> existingPositions = new();
        while (iterator.HasMoreResults)
        {
            FeedResponse<long>? response = await RetryPolicy.ExecuteAsync(
                async () => await iterator.ReadNextAsync(cancellationToken),
                cancellationToken);
            foreach (long position in response)
            {
                existingPositions.Add(position);
            }
        }

        return existingPositions;
    }

    /// <summary>
    ///     Deletes an event at the specified position from a brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The position of the event to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteEventAsync(
        BrookKey brookId,
        long position,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await Container.DeleteItemAsync<EventDocument>(
                position.ToString(CultureInfo.InvariantCulture),
                new(brookId.ToString()),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Event doesn't exist, which is the desired outcome for deletion
        }
    }

    /// <summary>
    ///     Deletes the pending head document for a brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeletePendingHeadAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await Container.DeleteItemAsync<HeadDocument>(
                "head-pending",
                new(brookId.ToString()),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Pending head doesn't exist, which is acceptable for deletion
        }
    }

    /// <summary>
    ///     Appends a batch of events to a brook starting at the specified position.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="startPosition">The starting position for the batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AppendEventBatchAsync(
        BrookKey brookId,
        IReadOnlyList<EventStorageModel> events,
        long startPosition,
        CancellationToken cancellationToken = default
    )
    {
        PartitionKey partitionKey = new(brookId.ToString());
        TransactionalBatch? batch = Container.CreateTransactionalBatch(partitionKey);
        long position = startPosition;
        // Track number of operations to avoid Cosmos 100-op limit
        int operationCount = 0;
        foreach (EventStorageModel eventStorageModel in events)
        {
            EventDocument eventDoc = new()
            {
                Id = position.ToString(CultureInfo.InvariantCulture),
                Type = "event",
                Position = position,
                BrookPartitionKey = brookId.ToString(),
                EventId = eventStorageModel.EventId,
                Source = eventStorageModel.Source,
                EventType = eventStorageModel.EventType,
                DataContentType = eventStorageModel.DataContentType,
                Data = eventStorageModel.Data,
                Time = eventStorageModel.Time,
            };
            batch.CreateItem(eventDoc);
            position++;
            operationCount++;
        }

        // Defensive: fail fast if we accidentally exceed 100 operations in a single batch
        if (operationCount > 100)
        {
            throw new InvalidOperationException(
                $"Transactional batch operation count {operationCount} exceeds Cosmos limit of 100.");
        }

        TransactionalBatchResponse response = await ExecuteBatchWithRetryAsync(batch, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string details = SummarizeBatchFailure(response);
            throw new InvalidOperationException($"Failed to append event batch: {details}");
        }
    }

    /// <summary>
    ///     Executes a transactional batch that updates the head position and appends events.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="currentHead">The current head position.</param>
    /// <param name="newPosition">The new head position after appending events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transactional batch response.</returns>
    public async Task<TransactionalBatchResponse> ExecuteTransactionalBatchAsync(
        BrookKey brookId,
        IReadOnlyList<EventStorageModel> events,
        BrookPosition currentHead,
        long newPosition,
        CancellationToken cancellationToken = default
    )
    {
        PartitionKey partitionKey = new(brookId.ToString());
        TransactionalBatch? batch = Container.CreateTransactionalBatch(partitionKey);
        HeadDocument headDoc = new()
        {
            Id = "head",
            Type = "head",
            Position = newPosition,
            BrookPartitionKey = brookId.ToString(),
        };
        if (currentHead.NotSet)
        {
            batch.CreateItem(headDoc);
        }
        else
        {
            batch.ReplaceItem("head", headDoc);
        }

        long position = currentHead.Value + 1;
        foreach (EventStorageModel eventStorageModel in events)
        {
            EventDocument eventDoc = new()
            {
                Id = position.ToString(CultureInfo.InvariantCulture),
                Type = "event",
                Position = position,
                BrookPartitionKey = brookId.ToString(),
                EventId = eventStorageModel.EventId,
                Source = eventStorageModel.Source,
                EventType = eventStorageModel.EventType,
                DataContentType = eventStorageModel.DataContentType,
                Data = eventStorageModel.Data,
                Time = eventStorageModel.Time,
            };
            batch.CreateItem(eventDoc);
            position++;
        }

        TransactionalBatchResponse response = await ExecuteBatchWithRetryAsync(batch, cancellationToken);
        return response;
    }

    /// <summary>
    ///     Queries events within a specified range from a brook.
    /// </summary>
    /// <param name="brookRange">The range of events to query.</param>
    /// <param name="batchSize">The batch size for querying.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of event storage models.</returns>
    public async IAsyncEnumerable<EventStorageModel> QueryEventsAsync(
        BrookRangeKey brookRange,
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        PartitionKey partitionKey = new(brookRange.ToBrookCompositeKey().ToString());
        QueryDefinition? query = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = 'event' AND c.position >= @start AND c.position <= @end ORDER BY c.position")
            .WithParameter("@start", brookRange.Start.Value)
            .WithParameter("@end", brookRange.End.Value);
        using FeedIterator<EventDocument>? iterator = Container.GetItemQueryIterator<EventDocument>(
            query,
            requestOptions: new()
            {
                PartitionKey = partitionKey,
                MaxItemCount = batchSize,
            });
        while (iterator.HasMoreResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FeedResponse<EventDocument>? response = await RetryPolicy.ExecuteAsync(
                async () => await iterator.ReadNextAsync(cancellationToken),
                cancellationToken);
            foreach (EventDocument? eventDoc in response)
            {
                yield return EventDocumentMapper.Map(eventDoc);
            }
        }
    }

    private async Task<TransactionalBatchResponse> ExecuteBatchWithRetryAsync(
        TransactionalBatch batch,
        CancellationToken cancellationToken
    )
    {
        Exception? last = null;
        for (int attempt = 0; attempt <= 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                TransactionalBatchResponse response = await batch.ExecuteAsync(cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                // Transient statuses to retry on
                if ((response.StatusCode == HttpStatusCode.TooManyRequests) ||
                    (response.StatusCode == HttpStatusCode.ServiceUnavailable) ||
                    (response.StatusCode == HttpStatusCode.RequestTimeout) ||
                    (response.StatusCode == HttpStatusCode.InternalServerError) ||
                    (response.StatusCode == HttpStatusCode.GatewayTimeout))
                {
                    TimeSpan delay = response.RetryAfter ?? TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return response; // non-transient failure; let caller decide
            }
            catch (CosmosException ex)
            {
                last = ex;
                // Surface 413 as a meaningful error
                if (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                {
                    throw new InvalidOperationException(
                        "Transactional batch request size exceeds maximum allowed limit. Consider reducing batch size.",
                        ex);
                }

                if (attempt == 3)
                {
                    throw;
                }

                TimeSpan delay = ex.RetryAfter ?? TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException("Transactional batch failed after retries", last);
    }

    private static string SummarizeBatchFailure(
        TransactionalBatchResponse response
    )
    {
        try
        {
            List<string> ops = new();
            for (int i = 0; i < response.Count; i++)
            {
                TransactionalBatchOperationResult r = response[i];
                if (!r.IsSuccessStatusCode)
                {
                    ops.Add($"#{i}:{(int)r.StatusCode}");
                }
            }

            string opsSummary = ops.Count > 0 ? string.Join(",", ops) : "none";
            return
                $"status={(int)response.StatusCode} retryAfter={response.RetryAfter?.TotalMilliseconds ?? 0}ms opFailures=[{opsSummary}] error={response.ErrorMessage}";
        }
        catch
        {
            return response.ErrorMessage ?? response.StatusCode.ToString();
        }
    }
}