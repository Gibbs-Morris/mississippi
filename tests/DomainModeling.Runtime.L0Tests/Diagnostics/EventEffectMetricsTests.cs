using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Runtime.Diagnostics;


namespace Mississippi.DomainModeling.Runtime.L0Tests.Diagnostics;

/// <summary>
///     Tests for <see cref="EventEffectMetrics" />.
/// </summary>
public sealed class EventEffectMetricsTests : IDisposable
{
    private readonly string aggregateType = Guid.NewGuid().ToString("N");

    private readonly MeterListener listener;

    private readonly ConcurrentQueue<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> measurements =
        new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventEffectMetricsTests" /> class.
    /// </summary>
    public EventEffectMetricsTests()
    {
        listener = new();
        listener.InstrumentPublished = (instrument, meterListener) =>
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
        foreach (KeyValuePair<string, object?> tag in tags)
        {
            if ((tag.Key == "aggregate.type") && tag.Value is string value && (value == aggregateType))
            {
                measurements.Enqueue((instrument.Name, measurement, tags.ToArray()));
                return;
            }
        }
    }

    /// <summary>
    ///     Error measurements from another aggregate do not replace the owned measurement.
    /// </summary>
    [Fact]
    public void RecordEffectErrorIgnoresForeignAggregate()
    {
        // Arrange
        EventEffectMetrics.RecordEffectError(aggregateType + "-foreign", "LifecycleEffect", "EffectLifecycleEvent");

        // Act
        EventEffectMetrics.RecordEffectError(aggregateType, "TestEffect", "TestEvent");
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags)[] snapshot = measurements.ToArray();
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) measurement =
            snapshot.FirstOrDefault(m => m.Name == "effect.execution.errors");
        Assert.NotEqual(default, measurement);
        Assert.Equal(1L, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "aggregate.type") && (t.Value?.ToString() == aggregateType));
        Assert.Contains(measurement.Tags, t => (t.Key == "effect.type") && (t.Value?.ToString() == "TestEffect"));
        Assert.Contains(measurement.Tags, t => (t.Key == "event.type") && (t.Value?.ToString() == "TestEvent"));
        Assert.Single(snapshot);
    }

    /// <summary>
    ///     Concurrent producers retain every owned sample and exclude foreign samples.
    /// </summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task RecordEffectErrorKeepsConcurrentOwnedMeasurementsAsync()
    {
        // Arrange
        const int producerCount = 4;
        const int measurementCount = 32;
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource[] ready = Enumerable.Range(0, producerCount)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();
        Task[] producers = Enumerable.Range(0, producerCount)
            .Select(producerIndex => Task.Run(async () =>
            {
                ready[producerIndex].SetResult();
                await start.Task.WaitAsync(TestContext.Current.CancellationToken);
                for (int measurementIndex = 0; measurementIndex < measurementCount; measurementIndex++)
                {
                    EventEffectMetrics.RecordEffectError("ForeignAggregate", "ForeignEffect", "ForeignEvent");
                    EventEffectMetrics.RecordEffectError(
                        aggregateType,
                        $"OwnedEffect-{producerIndex}-{measurementIndex}",
                        "ConcurrentEvent");
                }
            }))
            .ToArray();

        // Act
        await Task.WhenAll(ready.Select(producer => producer.Task));
        start.SetResult();
        await Task.WhenAll(producers);
        (string Name, object Value, KeyValuePair<string, object?>[] Tags)[] snapshot = measurements.ToArray();

        // Assert
        Assert.Equal(producerCount * measurementCount, snapshot.Length);
        Assert.All(
            snapshot,
            measurement =>
            {
                Assert.Equal("effect.execution.errors", measurement.Name);
                Assert.Equal(1L, measurement.Value);
                Assert.Contains(
                    measurement.Tags,
                    t => (t.Key == "aggregate.type") && (t.Value?.ToString() == aggregateType));
                Assert.Contains(
                    measurement.Tags,
                    t => (t.Key == "event.type") && (t.Value?.ToString() == "ConcurrentEvent"));
            });
        string[] expectedEffects = Enumerable.Range(0, producerCount)
            .SelectMany(producerIndex => Enumerable.Range(0, measurementCount)
                .Select(measurementIndex => $"OwnedEffect-{producerIndex}-{measurementIndex}"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        string?[] actualEffects = snapshot
            .Select(measurement => Assert.Single(measurement.Tags, t => t.Key == "effect.type").Value?.ToString())
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedEffects, actualEffects);
    }

    /// <summary>
    ///     RecordEffectError records error metric with correct tags.
    /// </summary>
    [Fact]
    public void RecordEffectErrorRecordsMetricWithTags()
    {
        // Act
        EventEffectMetrics.RecordEffectError(aggregateType, "TestEffect", "TestEvent");
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) measurement =
            measurements.ToArray().FirstOrDefault(m => m.Name == "effect.execution.errors");
        Assert.NotEqual(default, measurement);
        Assert.Equal(1L, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "aggregate.type") && (t.Value?.ToString() == aggregateType));
        Assert.Contains(measurement.Tags, t => (t.Key == "effect.type") && (t.Value?.ToString() == "TestEffect"));
        Assert.Contains(measurement.Tags, t => (t.Key == "event.type") && (t.Value?.ToString() == "TestEvent"));
    }

    /// <summary>
    ///     RecordEffectExecution records count and duration metrics.
    /// </summary>
    [Fact]
    public void RecordEffectExecutionRecordsCountAndDuration()
    {
        // Act
        EventEffectMetrics.RecordEffectExecution(aggregateType, "TestEffect", "TestEvent", 42.5, true);
        listener.RecordObservableInstruments();
        (string Name, object Value, KeyValuePair<string, object?>[] Tags)[] snapshot = measurements.ToArray();

        // Assert - count metric
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) countMeasurement =
            snapshot.FirstOrDefault(m => m.Name == "effect.execution.count");
        Assert.NotEqual(default, countMeasurement);
        Assert.Equal(1L, countMeasurement.Value);
        Assert.Contains(countMeasurement.Tags, t => (t.Key == "result") && (t.Value?.ToString() == "success"));

        // Assert - duration metric
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) durationMeasurement =
            snapshot.FirstOrDefault(m => m.Name == "effect.execution.duration");
        Assert.NotEqual(default, durationMeasurement);
        Assert.Equal(42.5, durationMeasurement.Value);
    }

    /// <summary>
    ///     RecordEffectExecution records failure result tag when not successful.
    /// </summary>
    [Fact]
    public void RecordEffectExecutionRecordsFailureResult()
    {
        // Act
        EventEffectMetrics.RecordEffectExecution(aggregateType, "TestEffect", "TestEvent", 10.0, false);
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) countMeasurement =
            measurements.ToArray().FirstOrDefault(m => m.Name == "effect.execution.count");
        Assert.NotEqual(default, countMeasurement);
        Assert.Contains(countMeasurement.Tags, t => (t.Key == "result") && (t.Value?.ToString() == "failure"));
    }

    /// <summary>
    ///     RecordEventYielded records yielded event metric with correct tags.
    /// </summary>
    [Fact]
    public void RecordEventYieldedRecordsMetricWithTags()
    {
        // Act
        EventEffectMetrics.RecordEventYielded(aggregateType, "TestEffect", "YieldedEvent");
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) measurement =
            measurements.ToArray().FirstOrDefault(m => m.Name == "effect.events.yielded");
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
    public void RecordIterationLimitReachedRecordsMetricWithTags()
    {
        // Act
        EventEffectMetrics.RecordIterationLimitReached(aggregateType, "test-key-123");
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) measurement =
            measurements.ToArray().FirstOrDefault(m => m.Name == "effect.iteration.limit");
        Assert.NotEqual(default, measurement);
        Assert.Equal(1L, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "aggregate.key") && (t.Value?.ToString() == "test-key-123"));
    }

    /// <summary>
    ///     RecordSlowEffect records slow effect metric with correct tags.
    /// </summary>
    [Fact]
    public void RecordSlowEffectRecordsMetricWithTags()
    {
        // Act
        EventEffectMetrics.RecordSlowEffect(aggregateType, "SlowEffect", "TestEvent");
        listener.RecordObservableInstruments();

        // Assert
        (string Name, object Value, KeyValuePair<string, object?>[] Tags) measurement =
            measurements.ToArray().FirstOrDefault(m => m.Name == "effect.execution.slow");
        Assert.NotEqual(default, measurement);
        Assert.Equal(1L, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "effect.type") && (t.Value?.ToString() == "SlowEffect"));
    }
}