using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.UxProjections.Diagnostics;


namespace Mississippi.EventSourcing.UxProjections.L0Tests.Diagnostics;

/// <summary>
///     Tests for UX projection metrics.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections")]
[AllureSubSuite("Metrics")]
public sealed class UxProjectionMetricsTests
{
    private sealed record MetricMeasurement(
        string InstrumentName,
        long LongValue,
        double DoubleValue,
        IReadOnlyDictionary<string, object?> Tags
    );

    /// <summary>
    ///     RecordCursorRead should emit metric with projection type tag.
    /// </summary>
    [Fact]
    [AllureFeature("Cursor Metrics")]
    public void RecordCursorReadEmitsMetricWithProjectionType()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == UxProjectionMetrics.MeterName)
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
        UxProjectionMetrics.RecordCursorRead("TestProjection");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "projection.cursor.reads") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("projection.type", out object? projType) &&
                           (projType as string == "TestProjection"));
    }

    /// <summary>
    ///     RecordNotificationSent should emit metric with projection type tag.
    /// </summary>
    [Fact]
    [AllureFeature("Notification Metrics")]
    public void RecordNotificationSentEmitsMetricWithProjectionType()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == UxProjectionMetrics.MeterName)
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
        UxProjectionMetrics.RecordNotificationSent("TestProjection");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "projection.notification.sent") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("projection.type", out object? projType) &&
                           (projType as string == "TestProjection"));
    }

    /// <summary>
    ///     RecordQuery should emit count and duration metrics.
    /// </summary>
    [Fact]
    [AllureFeature("Query Metrics")]
    public void RecordQueryEmitsCountAndDurationMetrics()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> longMeasurements = [];
        List<MetricMeasurement> doubleMeasurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == UxProjectionMetrics.MeterName)
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
        UxProjectionMetrics.RecordQuery("TestProjection", "latest", 50.5, true);
        Assert.Contains(
            longMeasurements,
            measurement => (measurement.InstrumentName == "projection.query.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("projection.type", out object? projType) &&
                           (projType as string == "TestProjection") &&
                           measurement.Tags.TryGetValue("query.type", out object? queryType) &&
                           (queryType as string == "latest"));
        Assert.Contains(
            doubleMeasurements,
            measurement => (measurement.InstrumentName == "projection.query.duration") &&
                           (Math.Abs(measurement.DoubleValue - 50.5) < 0.01));
    }

    /// <summary>
    ///     RecordQuery with no result should emit empty query metric.
    /// </summary>
    [Fact]
    [AllureFeature("Query Metrics")]
    public void RecordQueryWithNoResultEmitsEmptyMetric()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == UxProjectionMetrics.MeterName)
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
            // Ignore histogram measurements for this test
        });
        listener.Start();
        UxProjectionMetrics.RecordQuery("TestProjection", "versioned", 30.0, false);
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "projection.query.empty") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("projection.type", out object? projType) &&
                           (projType as string == "TestProjection"));
    }

    /// <summary>
    ///     RecordSubscription should emit metric with action tag.
    /// </summary>
    /// <param name="action">The subscription action.</param>
    [Theory]
    [InlineData("subscribe")]
    [InlineData("unsubscribe")]
    [AllureFeature("Subscription Metrics")]
    public void RecordSubscriptionEmitsMetricWithActionTag(
        string action
    )
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == UxProjectionMetrics.MeterName)
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
        UxProjectionMetrics.RecordSubscription(action);
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "projection.subscription.count") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("action", out object? actionTag) &&
                           (actionTag as string == action));
    }

    /// <summary>
    ///     RecordVersionCacheHit should emit metric with projection type tag.
    /// </summary>
    [Fact]
    [AllureFeature("Cache Metrics")]
    public void RecordVersionCacheHitEmitsMetricWithProjectionType()
    {
        using MeterListener listener = new();
        List<MetricMeasurement> measurements = [];
        listener.InstrumentPublished = (
            instrument,
            listener
        ) =>
        {
            if (instrument.Meter.Name == UxProjectionMetrics.MeterName)
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
        UxProjectionMetrics.RecordVersionCacheHit("TestProjection");
        Assert.Contains(
            measurements,
            measurement => (measurement.InstrumentName == "projection.version.cache.hits") &&
                           (measurement.LongValue == 1) &&
                           measurement.Tags.TryGetValue("projection.type", out object? projType) &&
                           (projType as string == "TestProjection"));
    }
}