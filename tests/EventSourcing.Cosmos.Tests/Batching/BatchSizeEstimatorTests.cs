using System.Collections.Immutable;
using System.Text;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Batching;
using Mississippi.EventSourcing.Cosmos.Storage;

using Newtonsoft.Json;


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
            Data = ImmutableArray.CreateRange(Enumerable.Range(0, 100).Select(i => (byte)i)),
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
            Data = ImmutableArray.CreateRange(largeData),
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
    ///     Ensures CreateSizeLimitedBatches yields no batches when provided an empty sequence.
    /// </summary>
    [Fact]
    public void CreateSizeLimitedBatchesWithNoEventsReturnsEmpty()
    {
        BatchSizeEstimator estimator = new();
        IReadOnlyList<IReadOnlyList<BrookEvent>> batches = estimator.CreateSizeLimitedBatches(
                Array.Empty<BrookEvent>(),
                10,
                10_000)
            .ToList();
        Assert.Empty(batches);
    }

    /// <summary>
    ///     Ensures CreateSizeLimitedBatches flushes based on size constraints even when under the max event count.
    /// </summary>
    [Fact]
    public void CreateSizeLimitedBatchesSplitsBySizeLimit()
    {
        BatchSizeEstimator estimator = new();
        BrookEvent template = new()
        {
            Id = "size",
            Source = "src",
            Type = "type",
            DataContentType = "application/octet-stream",
            Data = CreatePayload(256, 0x1A),
            Time = DateTimeOffset.FromUnixTimeSeconds(1),
        };
        BrookEvent[] events = [
            Clone(template),
            Clone(template),
            Clone(template),
        ];
        long maxSize = estimator.EstimateBatchSize(events.Take(2).ToList());
        Assert.True(estimator.EstimateBatchSize(events) > maxSize, "Three events should exceed the configured max size.");
        List<IReadOnlyList<BrookEvent>> batches = estimator.CreateSizeLimitedBatches(events, 10, maxSize).ToList();
        Assert.Equal(2, batches.Count);
        Assert.Equal(2, batches[0].Count);
        Assert.Single(batches[1]);
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
    public void EstimateBatchSizeSumsEventEstimatesPlusOverhead()
    {
        BatchSizeEstimator estimator = new();
        BrookEvent e1 = new()
        {
            Id = "1",
            Type = "A",
            DataContentType = "application/octet-stream",
            Data = ImmutableArray.Create<byte>(1, 2, 3, 4),
            Time = DateTimeOffset.FromUnixTimeSeconds(11),
        };
        BrookEvent e2 = new()
        {
            Id = "2",
            Type = "B",
            DataContentType = "application/octet-stream",
            Data = ImmutableArray.Create<byte>(5),
            Time = DateTimeOffset.FromUnixTimeSeconds(12),
        };
        long size = estimator.EstimateBatchSize(
            new List<BrookEvent>
            {
                e1,
                e2,
            });
        long overhead = estimator.EstimateBatchSize(Array.Empty<BrookEvent>());
        long expected = overhead + estimator.EstimateEventSize(e1) + estimator.EstimateEventSize(e2);
        Assert.Equal(expected, size);
    }

    /// <summary>
    ///     Ensures payloads exactly at the large-event threshold still use the serialization path.
    /// </summary>
    [Fact]
    public void EstimateEventSizeThresholdKeepsSerializationPath()
    {
        BatchSizeEstimator estimator = new();
        const int threshold = 10_000_000;
        BrookEvent ev = new()
        {
            Id = new('a', 8),
            Source = new('b', 6),
            Type = new('c', 4),
            DataContentType = "application/custom",
            Data = CreatePayload(threshold, 0x2A),
            Time = DateTimeOffset.FromUnixTimeSeconds(42),
        };
        long actual = estimator.EstimateEventSize(ev);
        EventDocument expectedDoc = new()
        {
            Id = "sample",
            Type = "event",
            Position = 1,
            EventId = ev.Id,
            Source = ev.Source,
            EventType = ev.Type,
            DataContentType = ev.DataContentType,
            Data = ev.Data.ToArray(),
            Time = ev.Time.Value,
        };
        string serialized = JsonConvert.SerializeObject(expectedDoc);
        long expectedSerializationEstimate = (long)(Encoding.UTF8.GetByteCount(serialized) * 1.3);
        long fallbackEstimate = ComputeFallbackEstimate(ev);
        Assert.Equal(expectedSerializationEstimate, actual);
        Assert.True(actual < fallbackEstimate, "Serialization estimate should be lower than heuristic fallback.");
    }

    /// <summary>
    ///     Ensures the fallback estimation path matches the documented heuristic when serialization is skipped.
    /// </summary>
    [Fact]
    public void EstimateEventSizeLargeEventMatchesFallbackFormula()
    {
        BatchSizeEstimator estimator = new();
        const int payloadLength = 10_000_001;
        BrookEvent ev = new()
        {
            Id = new('X', 5),
            Source = new('Y', 7),
            Type = new('Z', 3),
            DataContentType = "text/plain",
            Data = CreatePayload(payloadLength, 0x5A),
            Time = DateTimeOffset.FromUnixTimeSeconds(512),
        };
        long actual = estimator.EstimateEventSize(ev);
        long expected = ComputeFallbackEstimate(ev);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     Ensures size-based batching allows events whose combined size exactly matches the limit.
    /// </summary>
    [Fact]
    public void CreateSizeLimitedBatchesAllowsExactSizeBoundary()
    {
        BatchSizeEstimator estimator = new();
        BrookEvent template = new()
        {
            Id = "boundary",
            Source = "src",
            Type = "type",
            Data = CreatePayload(512, 0x33),
            Time = DateTimeOffset.UtcNow,
        };
        BrookEvent[] events = [
            Clone(template),
            Clone(template),
        ];
        long singleEventSize = estimator.EstimateEventSize(events[0]);
        long overhead = estimator.EstimateBatchSize(Array.Empty<BrookEvent>());
        long maxSize = overhead + (singleEventSize * events.Length);
        List<IReadOnlyList<BrookEvent>> batches = estimator.CreateSizeLimitedBatches(events, 10, maxSize).ToList();
        Assert.Single(batches);
        Assert.Equal(events.Length, batches[0].Count);
    }

    private static BrookEvent Clone(
        BrookEvent source
    ) =>
        new()
        {
            Id = source.Id,
            Source = source.Source,
            Type = source.Type,
            DataContentType = source.DataContentType,
            Data = source.Data,
            Time = source.Time,
        };

    private static ImmutableArray<byte> CreatePayload(
        int length,
        byte fill
    )
    {
        byte[] buffer = new byte[length];
        Array.Fill(buffer, fill);
        return ImmutableArray.Create(buffer);
    }

    private static long ComputeFallbackEstimate(
        BrookEvent brookEvent
    )
    {
        long size = 300;
        size += (brookEvent.Id?.Length ?? 0) * 2L;
        size += (brookEvent.Source?.Length ?? 0) * 2L;
        size += (brookEvent.Type?.Length ?? 0) * 2L;
        size += (brookEvent.DataContentType?.Length ?? 0) * 2L;
        long base64Size = brookEvent.Data.Length * 4L / 3L;
        size += base64Size + 64;
        size += 200;
        return (long)(size * 1.4);
    }
}