using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.EventSourcing.Cosmos.Batching;

internal interface IBatchSizeEstimator
{
    long EstimateBatchSize(IReadOnlyList<BrookEvent> events);
    long EstimateEventSize(BrookEvent brookEvent);
    IEnumerable<IReadOnlyList<BrookEvent>> CreateSizeLimitedBatches(IReadOnlyList<BrookEvent> events, int maxEventsPerBatch, long maxSizeBytes);
}