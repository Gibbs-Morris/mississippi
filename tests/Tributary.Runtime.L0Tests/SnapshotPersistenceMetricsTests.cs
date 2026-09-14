using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;


namespace Mississippi.Tributary.Runtime.L0Tests;

/// <summary>
///     Tests for snapshot persistence decision metrics.
/// </summary>
public sealed class SnapshotPersistenceMetricsTests
{
    private static List<MetricMeasurement> Capture(
        Action action
    )
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SnapshotMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagMap[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, measurement, tagMap));
        });
        listener.Start();
        action();
        return measurements;
    }

    private static bool HasSnapshotType(
        MetricMeasurement measurement,
        string expected
    ) =>
        measurement.Tags.TryGetValue("snapshot.type", out object? value) && (value as string == expected);

    private sealed record MetricMeasurement(
        string InstrumentName,
        long Value,
        IReadOnlyDictionary<string, object?> Tags
    );

    /// <summary>
    ///     Verifies that eligible and skipped decisions use separate counters.
    /// </summary>
    [Fact]
    public void PersistenceDecisionsEmitSeparateCounters()
    {
        List<MetricMeasurement> measurements = Capture(() =>
        {
            SnapshotMetrics.RecordPersistRequested("TestSnapshot");
            SnapshotMetrics.RecordPersistSkipped("TestSnapshot");
        });
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "snapshot.persist.requested") &&
                           (measurement.Value == 1) &&
                           HasSnapshotType(measurement, "TestSnapshot"));
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "snapshot.persist.skipped") &&
                           (measurement.Value == 1) &&
                           HasSnapshotType(measurement, "TestSnapshot"));
    }

    /// <summary>
    ///     Verifies that successful and failed persistence attempts use separate counters.
    /// </summary>
    [Fact]
    public void PersistenceResultsEmitSeparateCounters()
    {
        List<MetricMeasurement> measurements = Capture(() =>
        {
            SnapshotMetrics.RecordPersist("TestSnapshot", 1, true);
            SnapshotMetrics.RecordPersist("TestSnapshot", 1, false);
        });
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "snapshot.persist.succeeded") &&
                           (measurement.Value == 1) &&
                           HasSnapshotType(measurement, "TestSnapshot"));
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "snapshot.persist.failed") &&
                           (measurement.Value == 1) &&
                           HasSnapshotType(measurement, "TestSnapshot"));
    }
}