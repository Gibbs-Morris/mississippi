using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;


using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Diagnostics;


namespace Mississippi.EventSourcing.Brooks.L0Tests.Diagnostics;

/// <summary>
///     Tests for brook read/write metrics.
/// </summary>
public sealed class BrookMetricsTests
{
    private sealed record MetricMeasurement(
        string InstrumentName,
        long LongValue,
        double DoubleValue,
        IReadOnlyDictionary<string, object?> Tags
    );

    /// <summary>
    ///     Verifies cursor read metrics include read type tag.
    /// </summary>
    [Fact]
    public void RecordCursorReadEmitsMetricWithReadType()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == BrookMetrics.MeterName)
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
        BrookKey brookKey = new("TestBrook", "entity-1");
        BrookMetrics.RecordCursorRead(brookKey, "cached");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "brook.cursor.reads") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("brook.name", out object? brookName) &&
                           (brookName as string == "TestBrook") &&
                           measurement.Tags.TryGetValue("read.type", out object? readType) &&
                           (readType as string == "cached"));
    }

    /// <summary>
    ///     Verifies read metrics emit events, duration with read mode tag.
    /// </summary>
    [Fact]
    public void RecordReadEmitsAllMetrics()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == BrookMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, 0, measurement, tagMap));
        });
        listener.Start();
        BrookKey brookKey = new("TestBrook", "entity-1");
        BrookMetrics.RecordRead(brookKey, 10, 15.3, "batch");

        // Verify events counter
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "brook.reader.events") &&
                           (measurement.LongValue == 10) &&
                           measurement.Tags.TryGetValue("brook.name", out object? brookName) &&
                           (brookName as string == "TestBrook") &&
                           measurement.Tags.TryGetValue("read.mode", out object? readMode) &&
                           (readMode as string == "batch"));

        // Verify duration histogram
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "brook.reader.duration") &&
                           (Math.Abs(measurement.DoubleValue - 15.3) < 0.001) &&
                           measurement.Tags.TryGetValue("brook.name", out object? brookName) &&
                           (brookName as string == "TestBrook"));
    }

    /// <summary>
    ///     Verifies slice fan-out metrics are recorded.
    /// </summary>
    [Fact]
    public void RecordSliceFanOutEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == BrookMetrics.MeterName)
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
        BrookKey brookKey = new("TestBrook", "entity-1");
        BrookMetrics.RecordSliceFanOut(brookKey, 4);
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "brook.reader.slices") &&
                           (measurement.LongValue == 4) &&
                           measurement.Tags.TryGetValue("brook.name", out object? brookName) &&
                           (brookName as string == "TestBrook"));
    }

    /// <summary>
    ///     Verifies write metrics emit events, duration, and batch size.
    /// </summary>
    [Fact]
    public void RecordWriteEmitsAllMetrics()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == BrookMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, 0, measurement, tagMap));
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

            measurements.Add(new(instrument.Name, measurement, 0, tagMap));
        });
        listener.Start();
        BrookKey brookKey = new("TestBrook", "entity-1");
        BrookMetrics.RecordWrite(brookKey, 5, 42.5);

        // Verify events counter
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "brook.writer.events") &&
                           (measurement.LongValue == 5) &&
                           measurement.Tags.TryGetValue("brook.name", out object? brookName) &&
                           (brookName as string == "TestBrook"));

        // Verify duration histogram
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "brook.writer.duration") &&
                           (Math.Abs(measurement.DoubleValue - 42.5) < 0.001) &&
                           measurement.Tags.TryGetValue("brook.name", out object? brookName) &&
                           (brookName as string == "TestBrook"));

        // Verify batch size histogram
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "brook.writer.batch.size") &&
                           (measurement.LongValue == 5) &&
                           measurement.Tags.TryGetValue("brook.name", out object? brookName) &&
                           (brookName as string == "TestBrook"));
    }

    /// <summary>
    ///     Verifies write error metrics include error type tag.
    /// </summary>
    [Fact]
    public void RecordWriteErrorEmitsErrorMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == BrookMetrics.MeterName)
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
        BrookKey brookKey = new("TestBrook", "entity-1");
        BrookMetrics.RecordWriteError(brookKey, "ConcurrencyException");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "brook.writer.errors") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("brook.name", out object? brookName) &&
                           (brookName as string == "TestBrook") &&
                           measurement.Tags.TryGetValue("error.type", out object? errorType) &&
                           (errorType as string == "ConcurrencyException"));
    }

    /// <summary>
    ///     Verifies zero event counts are not recorded.
    /// </summary>
    [Fact]
    public void ZeroEventCountsAreNotRecorded()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == BrookMetrics.MeterName)
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
            measurements.Add(new(instrument.Name, measurement, 0, new Dictionary<string, object?>()));
        });
        listener.SetMeasurementEventCallback<double>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            measurements.Add(new(instrument.Name, 0, measurement, new Dictionary<string, object?>()));
        });
        listener.SetMeasurementEventCallback<int>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            measurements.Add(new(instrument.Name, measurement, 0, new Dictionary<string, object?>()));
        });
        listener.Start();
        BrookKey brookKey = new("TestBrook", "entity-1");

        // These should not emit metrics
        BrookMetrics.RecordWrite(brookKey, 0, 10);
        BrookMetrics.RecordRead(brookKey, 0, 5, "batch");
        BrookMetrics.RecordSliceFanOut(brookKey, 0);

        // Only non-zero counts should be recorded
        Assert.Empty(measurements);
    }
}