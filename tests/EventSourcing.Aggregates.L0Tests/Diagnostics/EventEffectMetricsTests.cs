using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Aggregates.Diagnostics;


namespace Mississippi.EventSourcing.Aggregates.L0Tests.Diagnostics;

/// <summary>
///     Tests for <see cref="EventEffectMetrics" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("EventEffectMetrics")]
public sealed class EventEffectMetricsTests : IDisposable
{
    private readonly MeterListener listener;

    private readonly List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> measurements = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventEffectMetricsTests" /> class.
    /// </summary>
    public EventEffectMetricsTests()
    {
        listener = new();
        listener.InstrumentPublished = (
            instrument,
            meterListener
        ) =>
        {
            if (instrument.Meter.Name == EventEffectMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>(OnMeasurement);
        listener.SetMeasurementEventCallback<double>(OnMeasurement);
        listener.Start();
    }

    /// <inheritdoc />
    public void Dispose() => listener.Dispose();

    private void OnMeasurement<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state
    )
        where T : struct
    {
        measurements.Add((instrument.Name, measurement, tags.ToArray()));
    }

    /// <summary>
    ///     RecordEffectError records error metric with correct tags.
    /// </summary>
    [Fact]
    [AllureFeature("Metrics")]
    public void RecordEffectErrorRecordsMetricWithTags()
    {
        // Act
        EventEffectMetrics.RecordEffectError("TestAggregate", "TestEffect", "TestEvent");
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) measurement =
            measurements.FirstOrDefault(m => m.Name == "effect.execution.errors");
        Assert.NotEqual(default, measurement);
        Assert.Equal(1L, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "aggregate.type") && (t.Value?.ToString() == "TestAggregate"));
        Assert.Contains(measurement.Tags, t => (t.Key == "effect.type") && (t.Value?.ToString() == "TestEffect"));
        Assert.Contains(measurement.Tags, t => (t.Key == "event.type") && (t.Value?.ToString() == "TestEvent"));
    }

    /// <summary>
    ///     RecordEffectExecution records count and duration metrics.
    /// </summary>
    [Fact]
    [AllureFeature("Metrics")]
    public void RecordEffectExecutionRecordsCountAndDuration()
    {
        // Act
        EventEffectMetrics.RecordEffectExecution("TestAggregate", "TestEffect", "TestEvent", 42.5, true);
        listener.RecordObservableInstruments();

        // Assert - count metric
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) countMeasurement =
            measurements.FirstOrDefault(m => m.Name == "effect.execution.count");
        Assert.NotEqual(default, countMeasurement);
        Assert.Equal(1L, countMeasurement.Value);
        Assert.Contains(countMeasurement.Tags, t => (t.Key == "result") && (t.Value?.ToString() == "success"));

        // Assert - duration metric
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) durationMeasurement =
            measurements.FirstOrDefault(m => m.Name == "effect.execution.duration");
        Assert.NotEqual(default, durationMeasurement);
        Assert.Equal(42.5, durationMeasurement.Value);
    }

    /// <summary>
    ///     RecordEffectExecution records failure result tag when not successful.
    /// </summary>
    [Fact]
    [AllureFeature("Metrics")]
    public void RecordEffectExecutionRecordsFailureResult()
    {
        // Act
        EventEffectMetrics.RecordEffectExecution("TestAggregate", "TestEffect", "TestEvent", 10.0, false);
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) countMeasurement =
            measurements.FirstOrDefault(m => m.Name == "effect.execution.count");
        Assert.NotEqual(default, countMeasurement);
        Assert.Contains(countMeasurement.Tags, t => (t.Key == "result") && (t.Value?.ToString() == "failure"));
    }

    /// <summary>
    ///     RecordEventYielded records yielded event metric with correct tags.
    /// </summary>
    [Fact]
    [AllureFeature("Metrics")]
    public void RecordEventYieldedRecordsMetricWithTags()
    {
        // Act
        EventEffectMetrics.RecordEventYielded("TestAggregate", "TestEffect", "YieldedEvent");
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) measurement =
            measurements.FirstOrDefault(m => m.Name == "effect.events.yielded");
        Assert.NotEqual(default, measurement);
        Assert.Equal(1L, measurement.Value);
        Assert.Contains(
            measurement.Tags,
            t => (t.Key == "yielded.event.type") && (t.Value?.ToString() == "YieldedEvent"));
    }

    /// <summary>
    ///     RecordIterationLimitReached records metric with aggregate key tag.
    /// </summary>
    [Fact]
    [AllureFeature("Metrics")]
    public void RecordIterationLimitReachedRecordsMetricWithTags()
    {
        // Act
        EventEffectMetrics.RecordIterationLimitReached("TestAggregate", "test-key-123");
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) measurement =
            measurements.FirstOrDefault(m => m.Name == "effect.iteration.limit");
        Assert.NotEqual(default, measurement);
        Assert.Equal(1L, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "aggregate.key") && (t.Value?.ToString() == "test-key-123"));
    }

    /// <summary>
    ///     RecordSlowEffect records slow effect metric with correct tags.
    /// </summary>
    [Fact]
    [AllureFeature("Metrics")]
    public void RecordSlowEffectRecordsMetricWithTags()
    {
        // Act
        EventEffectMetrics.RecordSlowEffect("TestAggregate", "SlowEffect", "TestEvent");
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) measurement =
            measurements.FirstOrDefault(m => m.Name == "effect.execution.slow");
        Assert.NotEqual(default, measurement);
        Assert.Equal(1L, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "effect.type") && (t.Value?.ToString() == "SlowEffect"));
    }
}