using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

using Mississippi.Inlet.Silo.Diagnostics;


namespace Mississippi.Inlet.Silo.L0Tests;

/// <summary>
///     Tests for <see cref="InletMetrics" />.
/// </summary>
public sealed class InletMetricsTests : IDisposable
{
    private readonly ConcurrentBag<(string Name, long Value, KeyValuePair<string, object?>[] Tags)>
        counterMeasurements = [];

    private readonly ConcurrentBag<(string Name, double Value, KeyValuePair<string, object?>[] Tags)>
        histogramMeasurements = [];

    private readonly MeterListener listener;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletMetricsTests" /> class.
    /// </summary>
    public InletMetricsTests()
    {
        listener = new()
        {
            InstrumentPublished = (
                instrument,
                listener
            ) =>
            {
                if (instrument.Meter.Name == InletMetrics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>(OnCounterMeasurement);
        listener.SetMeasurementEventCallback<double>(OnHistogramMeasurement);
        listener.Start();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        listener.Dispose();
    }

    private void OnCounterMeasurement(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state
    )
    {
        counterMeasurements.Add((instrument.Name, measurement, tags.ToArray()));
    }

    private void OnHistogramMeasurement(
        Instrument instrument,
        double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state
    )
    {
        histogramMeasurements.Add((instrument.Name, measurement, tags.ToArray()));
    }

    /// <summary>
    ///     InletMetrics MeterName should be correct.
    /// </summary>
    [Fact]
    public void MeterNameIsCorrect()
    {
        Assert.Equal("Mississippi.Inlet", InletMetrics.MeterName);
    }

    /// <summary>
    ///     RecordCursorEventReceived should record a counter measurement.
    /// </summary>
    [Fact]
    public void RecordCursorEventReceivedRecordsCounter()
    {
        // Arrange
        string brookName = "test-brook";

        // Act
        InletMetrics.RecordCursorEventReceived(brookName);
        listener.RecordObservableInstruments();

        // Assert
        (string Name, long Value, KeyValuePair<string, object?>[] Tags) measurement =
            counterMeasurements.FirstOrDefault(m => m.Name == "inlet.cursor.event.received");
        Assert.Equal(1, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "brook.name") && ((string?)t.Value == brookName));
    }

    /// <summary>
    ///     RecordNotificationError should record a counter measurement with tags.
    /// </summary>
    [Fact]
    public void RecordNotificationErrorRecordsCounterWithTags()
    {
        // Arrange
        string projectionPath = "/api/test";
        string errorType = "TestException";

        // Act
        InletMetrics.RecordNotificationError(projectionPath, errorType);
        listener.RecordObservableInstruments();

        // Assert
        (string Name, long Value, KeyValuePair<string, object?>[] Tags) measurement =
            counterMeasurements.FirstOrDefault(m => m.Name == "inlet.notification.errors");
        Assert.Equal(1, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "projection.path") && ((string?)t.Value == projectionPath));
        Assert.Contains(measurement.Tags, t => (t.Key == "error.type") && ((string?)t.Value == errorType));
    }

    /// <summary>
    ///     RecordNotificationSent should record counter and histogram measurements.
    /// </summary>
    [Fact]
    public void RecordNotificationSentRecordsCounterAndHistogram()
    {
        // Arrange
        string projectionPath = "/api/orders";
        double durationMs = 42.5;

        // Act
        InletMetrics.RecordNotificationSent(projectionPath, durationMs);
        listener.RecordObservableInstruments();

        // Assert - Counter
        (string Name, long Value, KeyValuePair<string, object?>[] Tags) counterMeasurement =
            counterMeasurements.FirstOrDefault(m => m.Name == "inlet.notification.sent");
        Assert.Equal(1, counterMeasurement.Value);
        Assert.Contains(
            counterMeasurement.Tags,
            t => (t.Key == "projection.path") && ((string?)t.Value == projectionPath));

        // Assert - Histogram
        (string Name, double Value, KeyValuePair<string, object?>[] Tags) histogramMeasurement =
            histogramMeasurements.FirstOrDefault(m => m.Name == "inlet.notification.duration");
        Assert.Equal(durationMs, histogramMeasurement.Value);
        Assert.Contains(
            histogramMeasurement.Tags,
            t => (t.Key == "projection.path") && ((string?)t.Value == projectionPath));
    }

    /// <summary>
    ///     RecordSubscription should record a counter measurement with action tag.
    /// </summary>
    [Fact]
    public void RecordSubscriptionRecordsCounterWithActionTag()
    {
        // Arrange
        string projectionPath = "/api/customers";
        string action = "subscribe";

        // Act
        InletMetrics.RecordSubscription(projectionPath, action);
        listener.RecordObservableInstruments();

        // Assert
        (string Name, long Value, KeyValuePair<string, object?>[] Tags) measurement =
            counterMeasurements.FirstOrDefault(m => m.Name == "inlet.subscription.count");
        Assert.Equal(1, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "projection.path") && ((string?)t.Value == projectionPath));
        Assert.Contains(measurement.Tags, t => (t.Key == "action") && ((string?)t.Value == action));
    }

    /// <summary>
    ///     RecordSubscription with unsubscribe action should record correctly.
    /// </summary>
    [Fact]
    public void RecordSubscriptionWithUnsubscribeRecordsCorrectly()
    {
        // Arrange
        string projectionPath = "/api/products";
        string action = "unsubscribe";

        // Act
        InletMetrics.RecordSubscription(projectionPath, action);
        listener.RecordObservableInstruments();

        // Assert
        (string Name, long Value, KeyValuePair<string, object?>[] Tags) measurement =
            counterMeasurements.LastOrDefault(m => m.Name == "inlet.subscription.count");
        Assert.Equal(1, measurement.Value);
        Assert.Contains(measurement.Tags, t => (t.Key == "action") && ((string?)t.Value == action));
    }
}