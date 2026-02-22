using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Mississippi.EventSourcing.Aggregates.Diagnostics;


namespace Mississippi.EventSourcing.Aggregates.L0Tests.Diagnostics;

/// <summary>
///     Tests for aggregate command execution metrics.
/// </summary>
public sealed class AggregateMetricsTests
{
    private sealed record MetricMeasurement(
        string InstrumentName,
        long LongValue,
        double DoubleValue,
        IReadOnlyDictionary<string, object?> Tags
    );

    /// <summary>
    ///     RecordCommandFailure should emit command count with failure result.
    /// </summary>
    [Fact]
    public void RecordCommandFailureEmitsCommandCount()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AggregateMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, tagDict));
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

            measurements.Add(new(instrument.Name, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        AggregateMetrics.RecordCommandFailure("TestAggregate", "CreateCommand", 100.5, "VALIDATION_ERROR");

        // Assert
        MetricMeasurement? commandMeasurement = measurements.Find(m =>
            (m.InstrumentName == "aggregate.command.count") &&
            m.Tags.TryGetValue("result", out object? result) &&
            ((string?)result == "failure"));
        Assert.NotNull(commandMeasurement);
        Assert.Equal(1, commandMeasurement.LongValue);
        Assert.Equal("TestAggregate", commandMeasurement.Tags["aggregate.type"]);
        Assert.Equal("CreateCommand", commandMeasurement.Tags["command.type"]);
    }

    /// <summary>
    ///     RecordCommandFailure should emit command duration histogram.
    /// </summary>
    [Fact]
    public void RecordCommandFailureEmitsDuration()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AggregateMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        AggregateMetrics.RecordCommandFailure("TestAggregate", "CreateCommand", 100.5, "VALIDATION_ERROR");

        // Assert
        MetricMeasurement? durationMeasurement =
            measurements.Find(m => m.InstrumentName == "aggregate.command.duration");
        Assert.NotNull(durationMeasurement);
        Assert.Equal(100.5, durationMeasurement.DoubleValue);
    }

    /// <summary>
    ///     RecordCommandFailure should emit command error count.
    /// </summary>
    [Fact]
    public void RecordCommandFailureEmitsErrorCount()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AggregateMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, tagDict));
        });
        listener.Start();

        // Act
        AggregateMetrics.RecordCommandFailure("TestAggregate", "CreateCommand", 100.5, "VALIDATION_ERROR");

        // Assert
        MetricMeasurement? errorMeasurement = measurements.Find(m => m.InstrumentName == "aggregate.command.errors");
        Assert.NotNull(errorMeasurement);
        Assert.Equal(1, errorMeasurement.LongValue);
        Assert.Equal("VALIDATION_ERROR", errorMeasurement.Tags["error.code"]);
    }

    /// <summary>
    ///     RecordCommandSuccess should emit command count with success result.
    /// </summary>
    [Fact]
    public void RecordCommandSuccessEmitsCommandCount()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AggregateMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, tagDict));
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

            measurements.Add(new(instrument.Name, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        AggregateMetrics.RecordCommandSuccess("OrderAggregate", "PlaceOrder", 50.0, 3);

        // Assert
        MetricMeasurement? commandMeasurement = measurements.Find(m =>
            (m.InstrumentName == "aggregate.command.count") &&
            m.Tags.TryGetValue("result", out object? result) &&
            ((string?)result == "success"));
        Assert.NotNull(commandMeasurement);
        Assert.Equal(1, commandMeasurement.LongValue);
        Assert.Equal("OrderAggregate", commandMeasurement.Tags["aggregate.type"]);
        Assert.Equal("PlaceOrder", commandMeasurement.Tags["command.type"]);
    }

    /// <summary>
    ///     RecordCommandSuccess should emit events produced count when events are produced.
    /// </summary>
    [Fact]
    public void RecordCommandSuccessEmitsEventsProduced()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AggregateMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, tagDict));
        });
        listener.Start();

        // Act
        AggregateMetrics.RecordCommandSuccess("OrderAggregate", "PlaceOrder", 50.0, 3);

        // Assert
        MetricMeasurement? eventMeasurement = measurements.Find(m => m.InstrumentName == "aggregate.events.produced");
        Assert.NotNull(eventMeasurement);
        Assert.Equal(3, eventMeasurement.LongValue);
    }

    /// <summary>
    ///     RecordCommandSuccess should not emit events produced count when no events are produced.
    /// </summary>
    [Fact]
    public void RecordCommandSuccessNoEventsProducedDoesNotEmitEventMetric()
    {
        // Use unique names to isolate from other tests running in parallel
        const string aggregateType = "NoEventsAggregate";
        const string commandType = "NoEventsCommand";
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AggregateMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, tagDict));
        });
        listener.Start();

        // Act
        AggregateMetrics.RecordCommandSuccess(aggregateType, commandType, 50.0, 0);

        // Assert - filter by unique aggregate/command types to isolate from parallel tests
        MetricMeasurement? eventMeasurement = measurements.Find(m =>
            (m.InstrumentName == "aggregate.events.produced") &&
            m.Tags.TryGetValue("aggregate.type", out object? aggType) &&
            ((string?)aggType == aggregateType) &&
            m.Tags.TryGetValue("command.type", out object? cmdType) &&
            ((string?)cmdType == commandType));
        Assert.Null(eventMeasurement);
    }

    /// <summary>
    ///     RecordConcurrencyConflict should emit concurrency conflict count.
    /// </summary>
    [Fact]
    public void RecordConcurrencyConflictEmitsCount()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AggregateMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, tagDict));
        });
        listener.Start();

        // Act
        AggregateMetrics.RecordConcurrencyConflict("InventoryAggregate");

        // Assert
        MetricMeasurement? conflictMeasurement = measurements.Find(m =>
            m.InstrumentName == "aggregate.concurrency.conflicts");
        Assert.NotNull(conflictMeasurement);
        Assert.Equal(1, conflictMeasurement.LongValue);
        Assert.Equal("InventoryAggregate", conflictMeasurement.Tags["aggregate.type"]);
    }

    /// <summary>
    ///     RecordStateFetch should emit state fetch duration histogram.
    /// </summary>
    [Fact]
    public void RecordStateFetchEmitsDuration()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AggregateMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, 0, measurement, tagDict));
        });
        listener.Start();

        // Act
        AggregateMetrics.RecordStateFetch("CustomerAggregate", 25.5);

        // Assert
        MetricMeasurement? fetchMeasurement = measurements.Find(m =>
            m.InstrumentName == "aggregate.state.fetch.duration");
        Assert.NotNull(fetchMeasurement);
        Assert.Equal(25.5, fetchMeasurement.DoubleValue);
        Assert.Equal("CustomerAggregate", fetchMeasurement.Tags["aggregate.type"]);
    }
}