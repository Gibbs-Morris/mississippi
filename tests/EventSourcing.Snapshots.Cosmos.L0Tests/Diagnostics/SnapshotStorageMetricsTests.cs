using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Snapshots.Cosmos.Diagnostics;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests.Diagnostics;

/// <summary>
///     Tests for snapshot storage metrics.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots Cosmos")]
[AllureSubSuite("Metrics")]
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
    [AllureFeature("Delete Metrics")]
    public void RecordDeleteEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
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
        SnapshotStorageMetrics.RecordDelete("TestSnapshot");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "cosmos.snapshot.delete.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot"));
    }

    /// <summary>
    ///     RecordPrune should not emit metric when count is zero or negative.
    /// </summary>
    /// <param name="prunedCount">The pruned count to test.</param>
    [Theory]
    [AllureFeature("Prune Metrics")]
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
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
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
        SnapshotStorageMetrics.RecordPrune("TestSnapshot", prunedCount);
        Assert.DoesNotContain(measurements, measurement => measurement.InstrumentName == "cosmos.snapshot.prune.count");
    }

    /// <summary>
    ///     RecordPrune should emit metric with pruned count.
    /// </summary>
    [Fact]
    [AllureFeature("Prune Metrics")]
    public void RecordPruneEmitsMetricWithCount()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
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
        SnapshotStorageMetrics.RecordPrune("TestSnapshot", 5);
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "cosmos.snapshot.prune.count") &&
                           (measurement.LongValue == 5) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot"));
    }

    /// <summary>
    ///     RecordRead should emit count and duration metrics when found.
    /// </summary>
    [Fact]
    [AllureFeature("Read Metrics")]
    public void RecordReadEmitsMetricsWhenFound()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
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
        SnapshotStorageMetrics.RecordRead("TestSnapshot", 50.0, true);
        Assert.Contains(
            longMeasurements,
            measurement => (measurement.InstrumentName == "cosmos.snapshot.read.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot") &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "found"));
        Assert.Contains(
            doubleMeasurements,
            measurement => (measurement.InstrumentName == "cosmos.snapshot.read.duration") &&
                           (Math.Abs(measurement.DoubleValue - 50.0) < 0.01));
    }

    /// <summary>
    ///     RecordRead should emit not_found result when not found.
    /// </summary>
    [Fact]
    [AllureFeature("Read Metrics")]
    public void RecordReadEmitsNotFoundResult()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
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
        listener.SetMeasurementEventCallback<double>((
            _,
            _,
            _,
            _
        ) =>
        {
            // Ignore histogram
        });
        listener.Start();
        SnapshotStorageMetrics.RecordRead("TestSnapshot", 25.0, false);
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "cosmos.snapshot.read.count") &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "not_found"));
    }

    /// <summary>
    ///     RecordWrite should not emit size metric when size is zero.
    /// </summary>
    [Fact]
    [AllureFeature("Write Metrics")]
    public void RecordWriteDoesNotEmitSizeWhenZero()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
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
        listener.SetMeasurementEventCallback<double>((
            _,
            _,
            _,
            _
        ) =>
        {
            // Ignore histogram
        });
        listener.Start();
        SnapshotStorageMetrics.RecordWrite("TestSnapshot", 50.0, true);
        Assert.DoesNotContain(measurements, measurement => measurement.InstrumentName == "cosmos.snapshot.size");
    }

    /// <summary>
    ///     RecordWrite should emit failure result on failure.
    /// </summary>
    [Fact]
    [AllureFeature("Write Metrics")]
    public void RecordWriteEmitsFailureResult()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
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
        listener.SetMeasurementEventCallback<double>((
            _,
            _,
            _,
            _
        ) =>
        {
            // Ignore histogram
        });
        listener.Start();
        SnapshotStorageMetrics.RecordWrite("TestSnapshot", 50.0, false);
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "cosmos.snapshot.write.count") &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "failure"));
    }

    /// <summary>
    ///     RecordWrite should emit count, duration, and size metrics on success.
    /// </summary>
    [Fact]
    [AllureFeature("Write Metrics")]
    public void RecordWriteEmitsMetricsWithSize()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotStorageMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
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
        SnapshotStorageMetrics.RecordWrite("TestSnapshot", 100.0, true, 4096L);
        Assert.Contains(
            longMeasurements,
            measurement => (measurement.InstrumentName == "cosmos.snapshot.write.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "success"));
        Assert.Contains(
            doubleMeasurements,
            measurement => (measurement.InstrumentName == "cosmos.snapshot.write.duration") &&
                           (Math.Abs(measurement.DoubleValue - 100.0) < 0.01));
        Assert.Contains(
            longMeasurements,
            measurement => (measurement.InstrumentName == "cosmos.snapshot.size") && (measurement.LongValue == 4096L));
    }
}