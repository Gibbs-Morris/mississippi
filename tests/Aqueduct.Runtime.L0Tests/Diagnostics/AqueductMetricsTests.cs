using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Mississippi.Aqueduct.Grains.Diagnostics;


namespace Mississippi.Aqueduct.Grains.L0Tests.Diagnostics;

/// <summary>
///     Tests for Aqueduct SignalR metrics.
/// </summary>
public sealed class AqueductMetricsTests
{
    private sealed record MetricMeasurement(
        string InstrumentName,
        long LongValue,
        double DoubleValue,
        int IntValue,
        IReadOnlyDictionary<string, object?> Tags
    );

    /// <summary>
    ///     RecordClientConnect should emit metric with hub name tag.
    /// </summary>
    [Fact]
    public void RecordClientConnectEmitsMetricWithHubName()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AqueductMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
        });
        listener.Start();
        AqueductMetrics.RecordClientConnect("TestHub");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "signalr.client.connect") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("hub.name", out object? hubName) &&
                           (hubName as string == "TestHub"));
    }

    /// <summary>
    ///     RecordClientDisconnect should emit metric with hub name tag.
    /// </summary>
    [Fact]
    public void RecordClientDisconnectEmitsMetricWithHubName()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AqueductMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
        });
        listener.Start();
        AqueductMetrics.RecordClientDisconnect("TestHub");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "signalr.client.disconnect") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("hub.name", out object? hubName) &&
                           (hubName as string == "TestHub"));
    }

    /// <summary>
    ///     RecordClientMessageSent should emit count and duration metrics.
    /// </summary>
    [Fact]
    public void RecordClientMessageSentEmitsCountAndDurationMetrics()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AqueductMetrics.MeterName)
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

            longMeasurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
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

            doubleMeasurements.Add(new(instrument.Name, 0, measurement, 0, tagMap));
        });
        listener.Start();
        AqueductMetrics.RecordClientMessageSent("TestHub", "SendMessage", 25.5);
        Assert.Contains(
            longMeasurements,
            measurement => (measurement.InstrumentName == "signalr.client.message.sent") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("hub.name", out object? hubName) &&
                           (hubName as string == "TestHub") &&
                           measurement.Tags.TryGetValue("method", out object? method) &&
                           (method as string == "SendMessage"));
        Assert.Contains(
            doubleMeasurements,
            measurement => (measurement.InstrumentName == "signalr.client.message.duration") &&
                           (Math.Abs(measurement.DoubleValue - 25.5) < 0.01));
    }

    /// <summary>
    ///     RecordDeadServers should emit metric only when count is positive.
    /// </summary>
    [Fact]
    public void RecordDeadServersEmitsMetricOnlyWhenPositive()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AqueductMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            _,
            _
        ) =>
        {
            measurements.Add(new(instrument.Name, measurement, 0, 0, new Dictionary<string, object?>()));
        });
        listener.Start();

        // Zero count should not emit - verify by checking no zero-value measurement exists
        // (parallel tests would emit positive values, so this assertion is isolated)
        AqueductMetrics.RecordDeadServers(0);
        Assert.DoesNotContain(measurements, m => (m.InstrumentName == "signalr.server.dead") && (m.LongValue == 0));

        // Positive count should emit with the exact value
        AqueductMetrics.RecordDeadServers(3);
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "signalr.server.dead") && (measurement.LongValue == 3));
    }

    /// <summary>
    ///     RecordGroupJoin should emit metric with hub name tag.
    /// </summary>
    [Fact]
    public void RecordGroupJoinEmitsMetricWithHubName()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AqueductMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
        });
        listener.Start();
        AqueductMetrics.RecordGroupJoin("TestHub");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "signalr.group.join") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("hub.name", out object? hubName) &&
                           (hubName as string == "TestHub"));
    }

    /// <summary>
    ///     RecordGroupLeave should emit metric with hub name tag.
    /// </summary>
    [Fact]
    public void RecordGroupLeaveEmitsMetricWithHubName()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AqueductMetrics.MeterName)
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

            measurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
        });
        listener.Start();
        AqueductMetrics.RecordGroupLeave("TestHub");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "signalr.group.leave") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("hub.name", out object? hubName) &&
                           (hubName as string == "TestHub"));
    }

    /// <summary>
    ///     RecordGroupMessageSent should emit count and fanout metrics.
    /// </summary>
    [Fact]
    public void RecordGroupMessageSentEmitsCountAndFanoutMetrics()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> intMeasurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AqueductMetrics.MeterName)
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

            longMeasurements.Add(new(instrument.Name, measurement, 0, 0, tagMap));
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

            intMeasurements.Add(new(instrument.Name, 0, 0, measurement, tagMap));
        });
        listener.Start();
        AqueductMetrics.RecordGroupMessageSent("TestHub", "BroadcastMessage", 15);
        Assert.Contains(
            longMeasurements,
            measurement => (measurement.InstrumentName == "signalr.group.message.sent") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("hub.name", out object? hubName) &&
                           (hubName as string == "TestHub") &&
                           measurement.Tags.TryGetValue("method", out object? method) &&
                           (method as string == "BroadcastMessage"));
        Assert.Contains(
            intMeasurements,
            measurement => (measurement.InstrumentName == "signalr.group.fanout.size") && (measurement.IntValue == 15));
    }

    /// <summary>
    ///     RecordServerHeartbeat should emit metric.
    /// </summary>
    [Fact]
    public void RecordServerHeartbeatEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AqueductMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            _,
            _
        ) =>
        {
            measurements.Add(new(instrument.Name, measurement, 0, 0, new Dictionary<string, object?>()));
        });
        listener.Start();
        AqueductMetrics.RecordServerHeartbeat();
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "signalr.server.heartbeat") && (measurement.LongValue == 1));
    }

    /// <summary>
    ///     RecordServerRegister should emit metric.
    /// </summary>
    [Fact]
    public void RecordServerRegisterEmitsMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == AqueductMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((
            instrument,
            measurement,
            _,
            _
        ) =>
        {
            measurements.Add(new(instrument.Name, measurement, 0, 0, new Dictionary<string, object?>()));
        });
        listener.Start();
        AqueductMetrics.RecordServerRegister();
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "signalr.server.register") && (measurement.LongValue == 1));
    }
}