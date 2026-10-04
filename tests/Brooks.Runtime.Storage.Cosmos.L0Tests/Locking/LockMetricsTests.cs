using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;

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
        string brookName = $"CASCADE|CHAT|CONVERSATION-{Guid.NewGuid():N}";
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.Start();

        // Act - key has pipe-separated segments with instance ID at end
        LockMetrics.RecordContentionWait($"{brookName}|demo-conversation-123");

        // Assert - should extract just the brook name portion
        MetricMeasurement? contentionMeasurement = Assert.Single(
            measurements.ToArray(),
            m => (m.Tags["lock.key"] as string == brookName) && (m.InstrumentName == "lock.contention.waits"));
        Assert.NotNull(contentionMeasurement);
        Assert.Equal(brookName, contentionMeasurement.Tags["lock.key"]);
    }

    /// <summary>
    ///     RecordAcquireFailure should emit acquire count with failure result.
    /// </summary>
    [Fact]
    public void RecordAcquireFailureEmitsAcquireCount()
    {
        string brookName = $"RecordAcquireFailureEmitsAcquireCount-{Guid.NewGuid():N}";
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, 0, measurement, 0, tagDict));
        });
        listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, 0, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireFailure($"{brookName}|id123", 5000.0, 3);

        // Assert
        MetricMeasurement? acquireMeasurement = Assert.Single(
            measurements.ToArray(),
            m => (m.Tags["lock.key"] as string == brookName) &&
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
        string brookName = $"RecordAcquireFailureEmitsDuration-{Guid.NewGuid():N}";
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, 0, measurement, 0, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireFailure($"{brookName}|id123", 5000.0, 3);

        // Assert
        MetricMeasurement? durationMeasurement = Assert.Single(
            measurements.ToArray(),
            m => (m.Tags["lock.key"] as string == brookName) && (m.InstrumentName == "lock.acquire.duration"));
        Assert.NotNull(durationMeasurement);
        Assert.Equal(5000.0, durationMeasurement.DoubleValue);
    }

    /// <summary>
    ///     RecordAcquireFailure should emit failure count.
    /// </summary>
    [Fact]
    public void RecordAcquireFailureEmitsFailureCount()
    {
        string brookName = $"RecordAcquireFailureEmitsFailureCount-{Guid.NewGuid():N}";
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireFailure($"{brookName}|id123", 5000.0, 3);

        // Assert
        MetricMeasurement? failureMeasurement = Assert.Single(
            measurements.ToArray(),
            m => (m.Tags["lock.key"] as string == brookName) && (m.InstrumentName == "lock.acquire.failures"));
        Assert.NotNull(failureMeasurement);
        Assert.Equal(1, failureMeasurement.LongValue);
    }

    /// <summary>
    ///     RecordAcquireSuccess should emit acquire count with success result.
    /// </summary>
    [Fact]
    public void RecordAcquireSuccessEmitsAcquireCount()
    {
        string brookName = $"RecordAcquireSuccessEmitsAcquireCount-{Guid.NewGuid():N}";
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, 0, measurement, 0, tagDict));
        });
        listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, 0, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireSuccess($"{brookName}|id456", 100.0, 1);

        // Assert
        MetricMeasurement? acquireMeasurement = Assert.Single(
            measurements.ToArray(),
            m => (m.Tags["lock.key"] as string == brookName) &&
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
        string brookName = $"RecordAcquireSuccessEmitsRetryAttempts-{Guid.NewGuid():N}";
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, 0, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordAcquireSuccess($"{brookName}|id456", 100.0, 2);

        // Assert
        MetricMeasurement? attemptsMeasurement = Assert.Single(
            measurements.ToArray(),
            m => (m.Tags["lock.key"] as string == brookName) && (m.InstrumentName == "lock.acquire.attempts"));
        Assert.NotNull(attemptsMeasurement);
        Assert.Equal(2, attemptsMeasurement.IntValue);
    }

    /// <summary>
    ///     RecordContentionWait should emit contention wait count.
    /// </summary>
    [Fact]
    public void RecordContentionWaitEmitsCount()
    {
        string brookName = $"RecordContentionWaitEmitsCount-{Guid.NewGuid():N}";
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, measurement, 0, 0, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordContentionWait($"{brookName}|id789");

        // Assert
        MetricMeasurement? contentionMeasurement = Assert.Single(
            measurements.ToArray(),
            m => (m.Tags["lock.key"] as string == brookName) && (m.InstrumentName == "lock.contention.waits"));
        Assert.NotNull(contentionMeasurement);
        Assert.Equal(1, contentionMeasurement.LongValue);
    }

    /// <summary>
    ///     RecordHeldDuration should emit held duration histogram.
    /// </summary>
    [Fact]
    public void RecordHeldDurationEmitsDuration()
    {
        string brookName = $"RecordHeldDurationEmitsDuration-{Guid.NewGuid():N}";
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, 0, measurement, 0, tagDict));
        });
        listener.Start();

        // Act
        LockMetrics.RecordHeldDuration($"{brookName}|id101", 1500.0);

        // Assert
        MetricMeasurement? heldMeasurement = Assert.Single(
            measurements.ToArray(),
            m => (m.Tags["lock.key"] as string == brookName) && (m.InstrumentName == "lock.held.duration"));
        Assert.NotNull(heldMeasurement);
        Assert.Equal(1500.0, heldMeasurement.DoubleValue);
    }

    /// <summary>
    ///     Held duration assertions should select the expected brook despite other lock measurements.
    /// </summary>
    [Fact]
    public void RecordHeldDurationSeparatesLocksWithDifferentBrookNames()
    {
        string brookName = $"{nameof(RecordHeldDurationSeparatesLocksWithDifferentBrookNames)}-{Guid.NewGuid():N}";
        string otherBrookName = $"OtherBrook-{Guid.NewGuid():N}";
        using MeterListener listener = new();
        ConcurrentQueue<MetricMeasurement> measurements = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LockMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            Dictionary<string, object?> tagDict = [];
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                tagDict[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new(instrument.Name, 0, measurement, 0, tagDict));
        });
        listener.Start();
        LockMetrics.RecordHeldDuration($"{otherBrookName}|other-instance", 3.7668);
        LockMetrics.RecordHeldDuration($"{brookName}|expected-instance", 1500.0);
        MetricMeasurement expected = Assert.Single(
            measurements.ToArray(),
            measurement => (measurement.InstrumentName == "lock.held.duration") &&
                           (measurement.Tags["lock.key"] as string == brookName));
        MetricMeasurement other = Assert.Single(
            measurements.ToArray(),
            measurement => (measurement.InstrumentName == "lock.held.duration") &&
                           (measurement.Tags["lock.key"] as string == otherBrookName));
        Assert.Equal(1500.0, expected.DoubleValue);
        Assert.Equal(3.7668, other.DoubleValue);
    }
}