using System.Collections.Generic;

using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.EventSourcing.Cosmos.Batching;

/// <summary>
///     Provides functionality for estimating batch sizes and creating size-limited batches for Cosmos DB operations.
/// </summary>
internal interface IBatchSizeEstimator
{
    /// <summary>
    ///     Creates size-limited batches from a collection of events based on maximum count and size constraints.
    /// </summary>
    /// <param name="events">The collection of events to batch.</param>
    /// <param name="maxEventsPerBatch">The maximum number of events allowed per batch.</param>
    /// <param name="maxSizeBytes">The maximum size in bytes allowed per batch.</param>
    /// <returns>An enumerable of size-limited event batches.</returns>
    IEnumerable<IReadOnlyList<BrookEvent>> CreateSizeLimitedBatches(
        IReadOnlyList<BrookEvent> events,
        int maxEventsPerBatch,
        long maxSizeBytes
    );

    /// <summary>
    ///     Estimates the total size in bytes for a batch of events.
    /// </summary>
    /// <param name="events">The collection of events to estimate the size for.</param>
    /// <returns>The estimated size in bytes for the entire batch.</returns>
    long EstimateBatchSize(
        IReadOnlyList<BrookEvent> events
    );

    /// <summary>
    ///     Estimates the size in bytes for a single event.
    /// </summary>
    /// <param name="brookEvent">The event to estimate the size for.</param>
    /// <returns>The estimated size in bytes for the event.</returns>
    long EstimateEventSize(
        BrookEvent brookEvent
    );
}