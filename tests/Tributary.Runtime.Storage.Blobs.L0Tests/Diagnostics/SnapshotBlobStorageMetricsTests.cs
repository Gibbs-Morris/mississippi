using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Mississippi.Tributary.Runtime.Storage.Blobs.Diagnostics;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.L0Tests.Diagnostics;

/// <summary>
///     Tests for Blob snapshot storage metrics.
/// </summary>
public sealed class SnapshotBlobStorageMetricsTests
{
    private sealed record MetricMeasurement(
        string InstrumentName,
        long LongValue,
        double DoubleValue,
        IReadOnlyDictionary<string, object?> Tags
    );

    /// <summary>
    ///     Verifies delete metrics use the Blob snapshot meter name and expected tags.
    /// </summary>
    [Fact]
    public void RecordDeleteEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
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

            measurements.Add(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordDelete("TestSnapshot");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "blob.snapshot.delete.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapshotType) &&
                           (snapshotType as string == "TestSnapshot"));
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
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
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

            measurements.Add(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordPrune("PruneSnapshot", prunedCount);
        Assert.DoesNotContain(
            measurements,
            measurement => (measurement.InstrumentName == "blob.snapshot.prune.count") &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapshotType) &&
                           (snapshotType as string == "PruneSnapshot"));
    }

    /// <summary>
    ///     Verifies positive prune counts emit metrics.
    /// </summary>
    [Fact]
    public void RecordPruneEmitsMetricWhenCountIsPositive()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
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

            measurements.Add(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordPrune("PruneSnapshot", 3);
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "blob.snapshot.prune.count") &&
                           (measurement.LongValue == 3) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapshotType) &&
                           (snapshotType as string == "PruneSnapshot"));
    }

    /// <summary>
    ///     Verifies reads emit count and duration metrics.
    /// </summary>
    [Fact]
    public void RecordReadEmitsMetricsWhenFound()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
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

            longMeasurements.Add(new(instrument.Name, measurement, 0, tagMap));
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

            doubleMeasurements.Add(new(instrument.Name, 0, measurement, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordRead("TestSnapshot", 50.0, true);
        Assert.Contains(
            longMeasurements,
            measurement => (measurement.InstrumentName == "blob.snapshot.read.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "found"));
        Assert.Contains(
            doubleMeasurements,
            measurement => (measurement.InstrumentName == "blob.snapshot.read.duration") &&
                           (Math.Abs(measurement.DoubleValue - 50.0) < 0.01));
    }

    /// <summary>
    ///     Verifies missing reads emit the not-found result tag.
    /// </summary>
    [Fact]
    public void RecordReadEmitsMetricsWhenNotFound()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
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

            measurements.Add(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordRead("TestSnapshot", 25.0, false);
        Assert.Contains(
            measurements,
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
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
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

            longMeasurements.Add(new(instrument.Name, measurement, 0, tagMap));
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

            doubleMeasurements.Add(new(instrument.Name, 0, measurement, tagMap));
        });
        listener.Start();
        SnapshotBlobStorageMetrics.RecordWrite("TestSnapshot", 100.0, true, 4096L);
        Assert.Contains(
            longMeasurements,
            measurement => (measurement.InstrumentName == "blob.snapshot.write.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "success"));
        Assert.Contains(
            doubleMeasurements,
            measurement => (measurement.InstrumentName == "blob.snapshot.write.duration") &&
                           (Math.Abs(measurement.DoubleValue - 100.0) < 0.01));
        Assert.Contains(
            longMeasurements,
            measurement => (measurement.InstrumentName == "blob.snapshot.size") && (measurement.LongValue == 4096L));
    }
}