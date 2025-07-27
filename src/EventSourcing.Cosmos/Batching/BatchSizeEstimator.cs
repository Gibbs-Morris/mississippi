using Mississippi.Core.Abstractions.Streams;
using Newtonsoft.Json;

namespace Mississippi.EventSourcing.Cosmos.Batching;

/// <summary>
/// Estimates the size of batches and individual events for Cosmos DB operations.
/// </summary>
internal class BatchSizeEstimator : IBatchSizeEstimator
{
    private const long BatchOverheadBytes = 2048;

    /// <summary>
    /// Estimates the total size of a batch of events in bytes.
    /// </summary>
    /// <param name="events">The events to estimate the size for.</param>
    /// <returns>The estimated size in bytes.</returns>
    public long EstimateBatchSize(IReadOnlyList<BrookEvent> events)
    {
        long totalSize = BatchOverheadBytes;

        foreach (var brookEvent in events)
        {
            totalSize += EstimateEventSize(brookEvent);
        }

        return totalSize;
    }

    /// <summary>
    /// Estimates the size of a single event in bytes.
    /// </summary>
    /// <param name="brookEvent">The event to estimate the size for.</param>
    /// <returns>The estimated size in bytes.</returns>
    public long EstimateEventSize(BrookEvent brookEvent)
    {
        try
        {
            var eventDoc = new Storage.EventDocument
            {
                Id = "sample",
                Type = "event",
                Position = 1,
                EventId = brookEvent.Id ?? string.Empty,
                Source = brookEvent.Source,
                EventType = brookEvent.Type ?? string.Empty,
                DataContentType = brookEvent.DataContentType,
                Data = brookEvent.Data.ToArray(),
                Time = brookEvent.Time ?? DateTimeOffset.UtcNow,
            };

            var serialized = JsonConvert.SerializeObject(eventDoc);
            var actualSize = System.Text.Encoding.UTF8.GetByteCount(serialized);

            return (long)(actualSize * 1.2);
        }
        catch (JsonException)
        {
            // Fall back to estimation if JSON serialization fails
            long size = 300;
            size += (brookEvent.Id?.Length ?? 0) * 2;
            size += (brookEvent.Source?.Length ?? 0) * 2;
            size += (brookEvent.Type?.Length ?? 0) * 2;
            size += (brookEvent.DataContentType?.Length ?? 0) * 2;
            size += brookEvent.Data.Length;
            size += 200;

            return size;
        }
        catch (OutOfMemoryException)
        {
            // Fall back to estimation if we run out of memory
            long size = 300;
            size += (brookEvent.Id?.Length ?? 0) * 2;
            size += (brookEvent.Source?.Length ?? 0) * 2;
            size += (brookEvent.Type?.Length ?? 0) * 2;
            size += (brookEvent.DataContentType?.Length ?? 0) * 2;
            size += brookEvent.Data.Length;
            size += 200;

            return size;
        }
    }

    /// <summary>
    /// Creates batches of events that respect size and count limits.
    /// </summary>
    /// <param name="events">The events to batch.</param>
    /// <param name="maxEventsPerBatch">The maximum number of events per batch.</param>
    /// <param name="maxSizeBytes">The maximum size in bytes per batch.</param>
    /// <returns>An enumerable of batches, each containing events within the specified limits.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a single event exceeds the maximum batch size.</exception>
    public IEnumerable<IReadOnlyList<BrookEvent>> CreateSizeLimitedBatches(IReadOnlyList<BrookEvent> events, int maxEventsPerBatch, long maxSizeBytes)
    {
        var currentBatch = new List<BrookEvent>();
        long currentBatchSize = BatchOverheadBytes;

        foreach (var brookEvent in events)
        {
            var eventSize = EstimateEventSize(brookEvent);

            if ((currentBatchSize + eventSize > maxSizeBytes || currentBatch.Count >= maxEventsPerBatch) && currentBatch.Count > 0)
            {
                yield return currentBatch;
                currentBatch = new List<BrookEvent>();
                currentBatchSize = BatchOverheadBytes;
            }

            if (eventSize > maxSizeBytes - BatchOverheadBytes)
            {
                throw new InvalidOperationException(
                    $"Single event is too large ({eventSize:N0} bytes). Maximum allowed size is {maxSizeBytes - BatchOverheadBytes:N0} bytes.");
            }

            currentBatch.Add(brookEvent);
            currentBatchSize += eventSize;
        }

        if (currentBatch.Count > 0)
        {
            yield return currentBatch;
        }
    }
}