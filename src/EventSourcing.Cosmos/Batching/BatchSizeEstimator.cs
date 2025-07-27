using Mississippi.Core.Abstractions.Streams;
using Newtonsoft.Json;

namespace Mississippi.EventSourcing.Cosmos.Batching;

internal class BatchSizeEstimator : IBatchSizeEstimator
{
    private const long BatchOverheadBytes = 2048;

    public long EstimateBatchSize(IReadOnlyList<BrookEvent> events)
    {
        long totalSize = BatchOverheadBytes;

        foreach (var brookEvent in events)
        {
            totalSize += EstimateEventSize(brookEvent);
        }

        return totalSize;
    }

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
                Time = brookEvent.Time ?? DateTimeOffset.UtcNow
            };

            var serialized = JsonConvert.SerializeObject(eventDoc);
            var actualSize = System.Text.Encoding.UTF8.GetByteCount(serialized);

            return (long)(actualSize * 1.2);
        }
        catch
        {
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