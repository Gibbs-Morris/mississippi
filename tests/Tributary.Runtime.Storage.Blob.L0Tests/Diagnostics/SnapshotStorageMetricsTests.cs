using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Mississippi.Tributary.Runtime.Storage.Blob.Diagnostics;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests.Diagnostics;

/// <summary>
///     Tests for Blob snapshot storage metrics.
/// </summary>
public sealed class SnapshotStorageMetricsTests
{
    private sealed record MetricMeasurement(
        string InstrumentName,
        long LongValue,
        double DoubleValue,
        IReadOnlyDictionary<string, object?> Tags
    );

    /// <summary>
    ///     RecordDelete should emit metric with snapshot type.
    /// </summary>
    [Fact]
    public void RecordDeleteEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (instrument, currentListener) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                currentListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        SnapshotStorageMetrics.RecordDelete("TestSnapshot");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "blob.snapshot.delete.count") &&
                           (measurement.LongValue == 1));
    }

    /// <summary>
    ///     RecordRead should emit count and duration metrics when found.
    /// </summary>
    [Fact]
    public void RecordReadEmitsMetricsWhenFound()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
        listener.InstrumentPublished = (instrument, currentListener) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                currentListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            longMeasurements.Add(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            doubleMeasurements.Add(new(instrument.Name, 0, measurement, tagMap));
        });
        listener.Start();
        SnapshotStorageMetrics.RecordRead("TestSnapshot", 50.0, true);
        Assert.Contains(longMeasurements, measurement => measurement.InstrumentName == "blob.snapshot.read.count");
        Assert.Contains(doubleMeasurements, measurement => measurement.InstrumentName == "blob.snapshot.read.duration");
    }

    /// <summary>
    ///     RecordWrite should emit count, duration, and size metrics on success.
    /// </summary>
    [Fact]
    public void RecordWriteEmitsMetricsWithSize()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
        listener.InstrumentPublished = (instrument, currentListener) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                currentListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            longMeasurements.Add(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            doubleMeasurements.Add(new(instrument.Name, 0, measurement, tagMap));
        });
        listener.Start();
        SnapshotStorageMetrics.RecordWrite("TestSnapshot", 100.0, true, 4096L);
        Assert.Contains(longMeasurements, measurement => measurement.InstrumentName == "blob.snapshot.write.count");
        Assert.Contains(doubleMeasurements, measurement => measurement.InstrumentName == "blob.snapshot.write.duration");
        Assert.Contains(longMeasurements, measurement => measurement.InstrumentName == "blob.snapshot.size");
    }
}
