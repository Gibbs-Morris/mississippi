using System.Text;

using Mississippi.EventSourcing.Abstractions.Brooks;
using Mississippi.EventSourcing.Cosmos.Storage;

using Newtonsoft.Json;


namespace Mississippi.EventSourcing.Cosmos.Batching;

/// <summary>
///     Estimates the size of batches and individual events for Cosmos DB operations.
/// </summary>
internal class BatchSizeEstimator : IBatchSizeEstimator
{
    // More realistic batch overhead based on Cosmos DB transactional batch structure
    // This includes: batch headers, response metadata, and internal overhead
    private const long BatchOverheadBytes = 4096; // Increased from 2048

    /// <summary>
    ///     Estimates the total size of a batch of events in bytes.
    /// </summary>
    /// <param name="events">The events to estimate the size for.</param>
    /// <returns>The estimated size in bytes.</returns>
    public long EstimateBatchSize(
        IReadOnlyList<BrookEvent> events
    )
    {
        long totalSize = BatchOverheadBytes;
        foreach (BrookEvent brookEvent in events)
        {
            totalSize += EstimateEventSize(brookEvent);
        }

        return totalSize;
    }

    /// <summary>
    ///     Estimates the size of a single event in bytes.
    /// </summary>
    /// <param name="brookEvent">The event to estimate the size for.</param>
    /// <returns>The estimated size in bytes.</returns>
    public long EstimateEventSize(
        BrookEvent brookEvent
    )
    {
        // First, do a quick size check to avoid memory issues with very large events
        long dataSize = brookEvent.Data.Length;
        if (dataSize > 10_000_000) // 10MB threshold
        {
            // For very large events, use estimation instead of serialization
            return EstimateEventSizeWithoutSerialization(brookEvent);
        }

        try
        {
            EventDocument eventDoc = new()
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
            string serialized = JsonConvert.SerializeObject(eventDoc);
            int actualSize = Encoding.UTF8.GetByteCount(serialized);
            return (long)(actualSize * 1.2); // 20% overhead for JSON formatting variations
        }
        catch (JsonException)
        {
            // Fall back to estimation if JSON serialization fails
            return EstimateEventSizeWithoutSerialization(brookEvent);
        }
        catch (OutOfMemoryException)
        {
            // Fall back to estimation if we run out of memory
            return EstimateEventSizeWithoutSerialization(brookEvent);
        }
        catch (Exception)
        {
            // For any other exception, fall back to estimation
            return EstimateEventSizeWithoutSerialization(brookEvent);
        }
    }

    /// <summary>
    ///     Creates batches of events that respect size and count limits.
    /// </summary>
    /// <param name="events">The events to batch.</param>
    /// <param name="maxEventsPerBatch">The maximum number of events per batch.</param>
    /// <param name="maxSizeBytes">The maximum size in bytes per batch.</param>
    /// <returns>An enumerable of batches, each containing events within the specified limits.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a single event exceeds the maximum batch size.</exception>
    public IEnumerable<IReadOnlyList<BrookEvent>> CreateSizeLimitedBatches(
        IReadOnlyList<BrookEvent> events,
        int maxEventsPerBatch,
        long maxSizeBytes
    )
    {
        List<BrookEvent> currentBatch = new();
        long currentBatchSize = BatchOverheadBytes;
        foreach (BrookEvent brookEvent in events)
        {
            long eventSize = EstimateEventSize(brookEvent);
            if ((((currentBatchSize + eventSize) > maxSizeBytes) || (currentBatch.Count >= maxEventsPerBatch)) &&
                (currentBatch.Count > 0))
            {
                yield return currentBatch;
                currentBatch = new();
                currentBatchSize = BatchOverheadBytes;
            }

            if (eventSize > (maxSizeBytes - BatchOverheadBytes))
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

    private long EstimateEventSizeWithoutSerialization(
        BrookEvent brookEvent
    )
    {
        // Base JSON structure overhead
        long size = 300;

        // String properties (UTF-8 encoding, so multiply by 2 for safety)
        size += (brookEvent.Id?.Length ?? 0) * 2;
        size += (brookEvent.Source?.Length ?? 0) * 2;
        size += (brookEvent.Type?.Length ?? 0) * 2;
        size += (brookEvent.DataContentType?.Length ?? 0) * 2;

        // Data size (base64 encoding adds ~33% overhead)
        size += ((brookEvent.Data.Length * 4) / 3) + 4;

        // DateTime serialization overhead
        size += 200;

        // JSON structure overhead and safety margin
        return (long)(size * 1.3);
    }
}