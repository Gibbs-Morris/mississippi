using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Mississippi.EventSourcing.Snapshots.Diagnostics;


namespace Mississippi.EventSourcing.Snapshots.L0Tests.Diagnostics;

/// <summary>
///     Tests for snapshot metrics.
/// </summary>
public sealed class SnapshotMetricsTests
{
    private sealed record MetricMeasurement(
        string InstrumentName,
        long LongValue,
        double DoubleValue,
        int IntValue,
        IReadOnlyDictionary<string, object?> Tags
    );

    /// <summary>
    ///     RecordActivation should emit failure metric on failure.
    /// </summary>
    [Fact]
    public void RecordActivationFailureEmitsFailureMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        object syncLock = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
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

            lock (syncLock)
            {
                measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
            }
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
        SnapshotMetrics.RecordActivation("TestSnapshot", 25.0, false);
        List<MetricMeasurement> snapshot;
        lock (syncLock)
        {
            snapshot = [.. measurements];
        }

        Assert.Contains(
            snapshot,
            measurement => (measurement.InstrumentName == "snapshot.activation.failures") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot"));
    }

    /// <summary>
    ///     RecordActivation should emit count and duration metrics for success.
    /// </summary>
    [Fact]
    public void RecordActivationSuccessEmitsMetrics()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
        object syncLock = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
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

            lock (syncLock)
            {
                longMeasurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
            }
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

            lock (syncLock)
            {
                doubleMeasurements.Add(new(instrument.Name, 0, measurement, 0, tagMap));
            }
        });
        listener.Start();
        SnapshotMetrics.RecordActivation("TestSnapshot", 50.0, true);
        List<MetricMeasurement> longSnapshot;
        List<MetricMeasurement> doubleSnapshot;
        lock (syncLock)
        {
            longSnapshot = [.. longMeasurements];
            doubleSnapshot = [.. doubleMeasurements];
        }

        Assert.Contains(
            longSnapshot,
            measurement => (measurement.InstrumentName == "snapshot.activation.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot") &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "success"));
        Assert.Contains(
            doubleSnapshot,
            measurement => (measurement.InstrumentName == "snapshot.activation.duration") &&
                           (Math.Abs(measurement.DoubleValue - 50.0) < 0.01));
        Assert.DoesNotContain(
            longSnapshot,
            measurement => measurement.InstrumentName == "snapshot.activation.failures");
    }

    /// <summary>
    ///     RecordBaseUsed should emit metric with snapshot type.
    /// </summary>
    [Fact]
    public void RecordBaseUsedEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        object syncLock = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
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

            lock (syncLock)
            {
                measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
            }
        });
        listener.Start();
        SnapshotMetrics.RecordBaseUsed("TestSnapshot");
        List<MetricMeasurement> snapshot;
        lock (syncLock)
        {
            snapshot = [.. measurements];
        }

        Assert.Contains(
            snapshot,
            measurement => (measurement.InstrumentName == "snapshot.base.used") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot"));
    }

    /// <summary>
    ///     RecordCacheHit should emit metric with snapshot type.
    /// </summary>
    [Fact]
    public void RecordCacheHitEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        object syncLock = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
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

            lock (syncLock)
            {
                measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
            }
        });
        listener.Start();
        SnapshotMetrics.RecordCacheHit("TestSnapshot");
        List<MetricMeasurement> snapshot;
        lock (syncLock)
        {
            snapshot = [.. measurements];
        }

        Assert.Contains(
            snapshot,
            measurement => (measurement.InstrumentName == "snapshot.cache.hits") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot"));
    }

    /// <summary>
    ///     RecordCacheMiss should emit metric with snapshot type.
    /// </summary>
    [Fact]
    public void RecordCacheMissEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        object syncLock = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
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

            lock (syncLock)
            {
                measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
            }
        });
        listener.Start();
        SnapshotMetrics.RecordCacheMiss("TestSnapshot");
        List<MetricMeasurement> snapshot;
        lock (syncLock)
        {
            snapshot = [.. measurements];
        }

        Assert.Contains(
            snapshot,
            measurement => (measurement.InstrumentName == "snapshot.cache.misses") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot"));
    }

    /// <summary>
    ///     RecordPersist should emit count and duration metrics.
    /// </summary>
    [Fact]
    public void RecordPersistEmitsMetrics()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
        object syncLock = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
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

            lock (syncLock)
            {
                longMeasurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
            }
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

            lock (syncLock)
            {
                doubleMeasurements.Add(new(instrument.Name, 0, measurement, 0, tagMap));
            }
        });
        listener.Start();
        SnapshotMetrics.RecordPersist("TestSnapshot", 75.0, true);
        List<MetricMeasurement> longSnapshot;
        List<MetricMeasurement> doubleSnapshot;
        lock (syncLock)
        {
            longSnapshot = [.. longMeasurements];
            doubleSnapshot = [.. doubleMeasurements];
        }

        Assert.Contains(
            longSnapshot,
            measurement => (measurement.InstrumentName == "snapshot.persist.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot") &&
                           measurement.Tags.TryGetValue("result", out object? result) &&
                           (result as string == "success"));
        Assert.Contains(
            doubleSnapshot,
            measurement => (measurement.InstrumentName == "snapshot.persist.duration") &&
                           (Math.Abs(measurement.DoubleValue - 75.0) < 0.01));
    }

    /// <summary>
    ///     RecordRebuild should emit duration and event count metrics.
    /// </summary>
    [Fact]
    public void RecordRebuildEmitsMetrics()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> doubleMeasurements = [];
        List<MetricMeasurement> intMeasurements = [];
        object syncLock = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
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

            lock (syncLock)
            {
                doubleMeasurements.Add(new(instrument.Name, 0, measurement, 0, tagMap));
            }
        });
        listener.SetMeasurementEventCallback<int>((
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

            lock (syncLock)
            {
                intMeasurements.Add(new(instrument.Name, 0, 0, measurement, tagMap));
            }
        });
        listener.Start();
        SnapshotMetrics.RecordRebuild("TestSnapshot", 100.0, 25);
        List<MetricMeasurement> doubleSnapshot;
        List<MetricMeasurement> intSnapshot;
        lock (syncLock)
        {
            doubleSnapshot = [.. doubleMeasurements];
            intSnapshot = [.. intMeasurements];
        }

        Assert.Contains(
            doubleSnapshot,
            measurement => (measurement.InstrumentName == "snapshot.rebuild.duration") &&
                           (Math.Abs(measurement.DoubleValue - 100.0) < 0.01));
        Assert.Contains(
            intSnapshot,
            measurement => (measurement.InstrumentName == "snapshot.rebuild.events") && (measurement.IntValue == 25));
    }

    /// <summary>
    ///     RecordReducerHashMismatch should emit metric with snapshot type.
    /// </summary>
    [Fact]
    public void RecordReducerHashMismatchEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        object syncLock = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
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

            lock (syncLock)
            {
                measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
            }
        });
        listener.Start();
        SnapshotMetrics.RecordReducerHashMismatch("TestSnapshot");
        List<MetricMeasurement> snapshot;
        lock (syncLock)
        {
            snapshot = [.. measurements];
        }

        Assert.Contains(
            snapshot,
            measurement => (measurement.InstrumentName == "snapshot.event_reducer.hash.mismatches") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot"));
    }

    /// <summary>
    ///     RecordStateSize should emit metric with snapshot type.
    /// </summary>
    [Fact]
    public void RecordStateSizeEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        object syncLock = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
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

            lock (syncLock)
            {
                measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
            }
        });
        listener.Start();
        SnapshotMetrics.RecordStateSize("TestSnapshot", 4096L);
        List<MetricMeasurement> snapshot;
        lock (syncLock)
        {
            snapshot = [.. measurements];
        }

        Assert.Contains(
            snapshot,
            measurement => (measurement.InstrumentName == "snapshot.state.size") &&
                           (measurement.LongValue == 4096L) &&
                           measurement.Tags.TryGetValue("snapshot.type", out object? snapType) &&
                           (snapType as string == "TestSnapshot"));
    }
}