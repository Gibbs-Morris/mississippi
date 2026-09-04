using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Mississippi.Tributary.Runtime.Storage.Blobs.Diagnostics;


namespace MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests.Diagnostics;

/// <summary>
///     Tests for Blob snapshot storage metrics.
/// </summary>
public sealed class SnapshotBlobStorageMetricsTests
{
    /// <summary>
    ///     Verifies delete metrics use the Blob snapshot meter name and expected tags.
    /// </summary>
    [Fact]
    public void RecordDeleteEmitsMetric()
    {
        const string expectedSnapshotType = nameof(RecordDeleteEmitsMetric);
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            meterListener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotBlobStorageMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (!tagMap.TryGetValue("snapshot.type", out object? measuredSnapshotType) ||
                !string.Equals(measuredSnapshotType as string, expectedSnapshotType, StringComparison.Ordinal))
            {
                return;
            }

            measurements.Enqueue(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordDelete(expectedSnapshotType);
        Assert.Contains(
            measurements.ToArray(),
            measurement => (measurement.InstrumentName == "blob.snapshot.delete.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapshotType) &&
                           (snapshotType as string == expectedSnapshotType));
    }

    /// <summary>
    ///     Verifies prune metrics are suppressed for non-positive counts.
    /// </summary>
    /// <param name="prunedCount">The pruned count to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RecordPruneDoesNotEmitWhenCountIsZeroOrNegative(
        int prunedCount
    )
    {
        const string expectedSnapshotType = nameof(RecordPruneDoesNotEmitWhenCountIsZeroOrNegative);
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            meterListener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotBlobStorageMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (!tagMap.TryGetValue("snapshot.type", out object? measuredSnapshotType) ||
                !string.Equals(measuredSnapshotType as string, expectedSnapshotType, StringComparison.Ordinal))
            {
                return;
            }

            measurements.Enqueue(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordPrune(expectedSnapshotType, prunedCount);
        Assert.DoesNotContain(
            measurements.ToArray(),
            measurement => (measurement.InstrumentName == "blob.snapshot.prune.count") &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapshotType) &&
                           (snapshotType as string == expectedSnapshotType));
    }

    /// <summary>
    ///     Verifies positive prune counts emit metrics.
    /// </summary>
    [Fact]
    public void RecordPruneEmitsMetricWhenCountIsPositive()
    {
        const string expectedSnapshotType = nameof(RecordPruneEmitsMetricWhenCountIsPositive);
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            meterListener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotBlobStorageMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (!tagMap.TryGetValue("snapshot.type", out object? measuredSnapshotType) ||
                !string.Equals(measuredSnapshotType as string, expectedSnapshotType, StringComparison.Ordinal))
            {
                return;
            }

            measurements.Enqueue(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordPrune(expectedSnapshotType, 3);
        Assert.Contains(
            measurements.ToArray(),
            measurement => (measurement.InstrumentName == "blob.snapshot.prune.count") &&
                           (measurement.LongValue == 3) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapshotType) &&
                           (snapshotType as string == expectedSnapshotType));
    }

    /// <summary>
    ///     Verifies reads emit count and duration metrics.
    /// </summary>
    [Fact]
    public void RecordReadEmitsMetricsWhenFound()
    {
        const string expectedSnapshotType = nameof(RecordReadEmitsMetricsWhenFound);
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> longMeasurements = new();
        ConcurrentQueue<MetricMeasurement> doubleMeasurements = new();
        listener.InstrumentPublished = (
            instrument,
            meterListener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotBlobStorageMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (!tagMap.TryGetValue("snapshot.type", out object? measuredSnapshotType) ||
                !string.Equals(measuredSnapshotType as string, expectedSnapshotType, StringComparison.Ordinal))
            {
                return;
            }

            longMeasurements.Enqueue(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.SetMeasurementEventCallback<double>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (!tagMap.TryGetValue("snapshot.type", out object? measuredSnapshotType) ||
                !string.Equals(measuredSnapshotType as string, expectedSnapshotType, StringComparison.Ordinal))
            {
                return;
            }

            doubleMeasurements.Enqueue(new(instrument.Name, 0, measurement, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordRead(expectedSnapshotType, 50.0, true);
        Assert.Contains(
            longMeasurements.ToArray(),
            measurement => (measurement.InstrumentName == "blob.snapshot.read.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "found"));
        Assert.Contains(
            doubleMeasurements.ToArray(),
            measurement => (measurement.InstrumentName == "blob.snapshot.read.duration") &&
                           (Math.Abs(measurement.DoubleValue - 50.0) < 0.01));
    }

    /// <summary>
    ///     Verifies missing reads emit the not-found result tag.
    /// </summary>
    [Fact]
    public void RecordReadEmitsMetricsWhenNotFound()
    {
        const string expectedSnapshotType = nameof(RecordReadEmitsMetricsWhenNotFound);
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            meterListener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotBlobStorageMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (!tagMap.TryGetValue("snapshot.type", out object? measuredSnapshotType) ||
                !string.Equals(measuredSnapshotType as string, expectedSnapshotType, StringComparison.Ordinal))
            {
                return;
            }

            measurements.Enqueue(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordRead(expectedSnapshotType, 25.0, false);
        Assert.Contains(
            measurements.ToArray(),
            measurement => (measurement.InstrumentName == "blob.snapshot.read.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "not_found"));
    }

    /// <summary>
    ///     Verifies writes emit count, duration, and size metrics.
    /// </summary>
    [Fact]
    public void RecordWriteEmitsMetricsWithSize()
    {
        const string expectedSnapshotType = nameof(RecordWriteEmitsMetricsWithSize);
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> longMeasurements = new();
        ConcurrentQueue<MetricMeasurement> doubleMeasurements = new();
        listener.InstrumentPublished = (
            instrument,
            meterListener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotBlobStorageMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (!tagMap.TryGetValue("snapshot.type", out object? measuredSnapshotType) ||
                !string.Equals(measuredSnapshotType as string, expectedSnapshotType, StringComparison.Ordinal))
            {
                return;
            }

            longMeasurements.Enqueue(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.SetMeasurementEventCallback<double>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            if (!tagMap.TryGetValue("snapshot.type", out object? measuredSnapshotType) ||
                !string.Equals(measuredSnapshotType as string, expectedSnapshotType, StringComparison.Ordinal))
            {
                return;
            }

            doubleMeasurements.Enqueue(new(instrument.Name, 0, measurement, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordWrite(expectedSnapshotType, 100.0, true, 4096L);
        Assert.Contains(
            longMeasurements.ToArray(),
            measurement => (measurement.InstrumentName == "blob.snapshot.write.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "success"));
        Assert.Contains(
            doubleMeasurements.ToArray(),
            measurement => (measurement.InstrumentName == "blob.snapshot.write.duration") &&
                           (Math.Abs(measurement.DoubleValue - 100.0) < 0.01));
        Assert.Contains(
            longMeasurements.ToArray(),
            measurement => (measurement.InstrumentName == "blob.snapshot.size") && (measurement.LongValue == 4096L));
    }
}