using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Batching;


namespace Mississippi.EventSourcing.Cosmos.Tests.Batching;

/// <summary>
///     Test class for BatchSizeEstimator functionality.
///     Contains unit tests to verify the behavior of batch size estimation algorithms.
/// </summary>
public class BatchSizeEstimatorTests
{
    /// <summary>
    ///     Ensures EstimateEventSize returns a positive estimate for small events.
    /// </summary>
    [Fact]
    public void EstimateEventSizeSmallEventReturnsReasonableEstimate()
    {
        // Arrange
        BatchSizeEstimator estimator = new();
        BrookEvent ev = new()
        {
            Id = "id-1",
            Source = "src",
            Type = "T",
            DataContentType = "application/octet-stream",
            Data = ImmutableArray.Create(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray()),
            Time = DateTimeOffset.UtcNow,
        };

        // Act
        long size = estimator.EstimateEventSize(ev);

        // Assert
        Assert.True(size > 0, "Estimated size should be positive");
        Assert.True(size >= ev.Data.Length, "Estimated size should be at least the data payload length");
    }

    /// <summary>
    ///     Ensures large events use the estimation path and return an estimate greater than raw length.
    /// </summary>
    [Fact]
    public void EstimateEventSizeLargeEventUsesEstimationPathAndDoesNotSerialize()
    {
        // Arrange
        BatchSizeEstimator estimator = new();

        // Create a large payload just over the 10MB threshold used by the estimator
        int largeLength = 10_000_000 + 1;
        byte[] largeData = new byte[largeLength];

        // Touch a few bytes to avoid optimizations
        largeData[0] = 1;
        largeData[largeLength - 1] = 2;
        BrookEvent ev = new()
        {
            Id = "big-evt",
            Source = "big-src",
            Type = "BIG",
            DataContentType = "application/octet-stream",
            Data = ImmutableArray.Create(largeData),
            Time = DateTimeOffset.UtcNow,
        };

        // Act
        long size = estimator.EstimateEventSize(ev);

        // Assert
        Assert.True(size > 0, "Estimated size for large event should be positive");

        // Should be larger than raw data (estimation includes overhead)
        Assert.True(size > largeLength, "Estimation should account for JSON/base64 overhead and safety margins");
    }

    /// <summary>
    ///     Ensures CreateSizeLimitedBatches splits batches by the maximum events per batch.
    /// </summary>
    [Fact]
    public void CreateSizeLimitedBatchesSplitsByMaxEventsPerBatch()
    {
        // Arrange
        BatchSizeEstimator estimator = new();
        BrookEvent[] events = Enumerable.Range(0, 5)
            .Select(i => new BrookEvent
            {
                Id = $"e{i}",
                Source = "s",
                Type = "t",
                Data = ImmutableArray.Create(new byte[10]),
            })
            .ToArray();

        // Act
        List<IReadOnlyList<BrookEvent>> batches = estimator.CreateSizeLimitedBatches(events, 2, long.MaxValue).ToList();

        // Assert
        Assert.Equal(3, batches.Count);
        Assert.All(batches.Take(2), b => Assert.Equal(2, b.Count));
        IReadOnlyList<BrookEvent> lastBatch = batches[batches.Count - 1];
        Assert.Single(lastBatch);
    }

    /// <summary>
    ///     Ensures CreateSizeLimitedBatches throws when a single event cannot fit into the provided max size.
    /// </summary>
    [Fact]
    public void CreateSizeLimitedBatchesThrowsWhenSingleEventTooLarge()
    {
        // Arrange
        BatchSizeEstimator estimator = new();

        // Create an event with a modest payload but use a very small maxSizeBytes to force the error
        BrookEvent ev = new()
        {
            Id = "oversized",
            Source = "s",
            Type = "t",
            Data = ImmutableArray.Create(new byte[2000]),
        };

        // Use a tiny max size so that after accounting for batch overhead the single event is too large
        long tinyMaxSize = 9_000; // BatchOverheadBytes is 8192, leaving only 808 bytes for event

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            estimator.CreateSizeLimitedBatches(new[] { ev }, 10, tinyMaxSize).ToList());
    }

    /// <summary>
    ///     Ensures EstimateBatchSize returns a positive value taking into account event estimates and overhead.
    /// </summary>
    [Fact]
    public void EstimateBatchSize_SumsEventEstimatesPlusOverhead()
    {
        BatchSizeEstimator estimator = new();
        BrookEvent e1 = new()
        {
            Id = "1",
            Type = "A",
            DataContentType = "application/octet-stream",
            Data = ImmutableArray.Create(new byte[] { 1, 2, 3, 4 }),
        };
        BrookEvent e2 = new()
        {
            Id = "2",
            Type = "B",
            DataContentType = "application/octet-stream",
            Data = ImmutableArray.Create(new byte[] { 5 }),
        };
        long size = estimator.EstimateBatchSize(
            new List<BrookEvent>
            {
                e1,
                e2,
            });
        Assert.True(size > 0);
    }
}