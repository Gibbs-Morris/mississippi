using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Mississippi.EventSourcing.Brooks.Cosmos.Locking;


namespace Mississippi.EventSourcing.Brooks.Cosmos.L0Tests.Locking;

/// <summary>
///     Tests for distributed lock metrics.
/// </summary>
public sealed class LockMetricsTests
{
    private sealed record MetricMeasurement(
        string InstrumentName,
        long LongValue,
        double DoubleValue,
        int IntValue,
        IReadOnlyDictionary<string, object?> Tags
    );

    /// <summary>
    ///     Lock key sanitization should extract brook name from full key.
    /// </summary>
    [Fact]
    public void LockKeySanitizationExtractsBrookName()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
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
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.Start();

        // Act - key has pipe-separated segments with instance ID at end
        LockMetrics.RecordContentionWait("CASCADE|CHAT|CONVERSATION|demo-conversation-123");

        // Assert - should extract just the brook name portion
        MetricMeasurement? contentionMeasurement = measurements.Find(m => m.InstrumentName == "lock.contention.waits");
        Assert.NotNull(contentionMeasurement);
        Assert.Equal("CASCADE|CHAT|CONVERSATION", contentionMeasurement.Tags["lock.key"]);
    }

    /// <summary>
    ///     RecordAcquireFailure should emit acquire count with failure result.
    /// </summary>
    [Fact]
    public void RecordAcquireFailureEmitsAcquireCount()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
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
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.SetMeasurementEventCallback<double>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, 0, measurement, 0, tagDict));
        });
        listener.SetMeasurementEventCallback<int>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, 0, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireFailure("CASCADE|CHAT|CONVERSATION|id123", 5000.0, 3);

        // Assert
        MetricMeasurement? acquireMeasurement = measurements.Find(m =>
            (m.InstrumentName == "lock.acquire.count") &&
            m.Tags.TryGetValue("result", out object? result) &&
            ((string?)result == "failure"));
        Assert.NotNull(acquireMeasurement);
        Assert.Equal(1, acquireMeasurement.LongValue);
    }

    /// <summary>
    ///     RecordAcquireFailure should emit acquire duration histogram.
    /// </summary>
    [Fact]
    public void RecordAcquireFailureEmitsDuration()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
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
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, 0, measurement, 0, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireFailure("CASCADE|CHAT|CONVERSATION|id123", 5000.0, 3);

        // Assert
        MetricMeasurement? durationMeasurement = measurements.Find(m => m.InstrumentName == "lock.acquire.duration");
        Assert.NotNull(durationMeasurement);
        Assert.Equal(5000.0, durationMeasurement.DoubleValue);
    }

    /// <summary>
    ///     RecordAcquireFailure should emit failure count.
    /// </summary>
    [Fact]
    public void RecordAcquireFailureEmitsFailureCount()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
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
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireFailure("CASCADE|CHAT|CONVERSATION|id123", 5000.0, 3);

        // Assert
        MetricMeasurement? failureMeasurement = measurements.Find(m => m.InstrumentName == "lock.acquire.failures");
        Assert.NotNull(failureMeasurement);
        Assert.Equal(1, failureMeasurement.LongValue);
    }

    /// <summary>
    ///     RecordAcquireSuccess should emit acquire count with success result.
    /// </summary>
    [Fact]
    public void RecordAcquireSuccessEmitsAcquireCount()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
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
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.SetMeasurementEventCallback<double>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, 0, measurement, 0, tagDict));
        });
        listener.SetMeasurementEventCallback<int>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, 0, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireSuccess("CASCADE|CHAT|CONVERSATION|id456", 100.0, 1);

        // Assert
        MetricMeasurement? acquireMeasurement = measurements.Find(m =>
            (m.InstrumentName == "lock.acquire.count") &&
            m.Tags.TryGetValue("result", out object? result) &&
            ((string?)result == "success"));
        Assert.NotNull(acquireMeasurement);
        Assert.Equal(1, acquireMeasurement.LongValue);
    }

    /// <summary>
    ///     RecordAcquireSuccess should emit retry attempts histogram.
    /// </summary>
    [Fact]
    public void RecordAcquireSuccessEmitsRetryAttempts()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((
            instrument,
            measurement,
            tags,
            _
        ) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, 0, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireSuccess("CASCADE|CHAT|CONVERSATION|id456", 100.0, 2);

        // Assert
        MetricMeasurement? attemptsMeasurement = measurements.Find(m => m.InstrumentName == "lock.acquire.attempts");
        Assert.NotNull(attemptsMeasurement);
        Assert.Equal(2, attemptsMeasurement.IntValue);
    }

    /// <summary>
    ///     RecordContentionWait should emit contention wait count.
    /// </summary>
    [Fact]
    public void RecordContentionWaitEmitsCount()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
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
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordContentionWait("CASCADE|CHAT|CONVERSATION|id789");

        // Assert
        MetricMeasurement? contentionMeasurement = measurements.Find(m => m.InstrumentName == "lock.contention.waits");
        Assert.NotNull(contentionMeasurement);
        Assert.Equal(1, contentionMeasurement.LongValue);
    }

    /// <summary>
    ///     RecordHeldDuration should emit held duration histogram.
    /// </summary>
    [Fact]
    public void RecordHeldDurationEmitsDuration()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
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
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Add(new(instrument.Name, 0, measurement, 0, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordHeldDuration("CASCADE|CHAT|CONVERSATION|id101", 1500.0);

        // Assert
        MetricMeasurement? heldMeasurement = measurements.Find(m => m.InstrumentName == "lock.held.duration");
        Assert.NotNull(heldMeasurement);
        Assert.Equal(1500.0, heldMeasurement.DoubleValue);
    }
}