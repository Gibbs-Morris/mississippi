using System.Diagnostics.Metrics;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Captures snapshot metrics emitted by the in-process test host.
/// </summary>
internal sealed class SnapshotMetricCapture : IDisposable
{
    private const string SnapshotMeterName = "Mississippi.Tributary.Runtime";

    private readonly MeterListener listener = new();

    private readonly List<SnapshotMetricMeasurement> measurements = [];

    private readonly object syncLock = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotMetricCapture" /> class.
    /// </summary>
    public SnapshotMetricCapture()
    {
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == SnapshotMeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            Record(instrument.Name, measurement, tags));
        listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, _) =>
            Record(instrument.Name, measurement, tags));
        listener.Start();
    }

    /// <inheritdoc />
    public void Dispose() => listener.Dispose();

    /// <summary>
    ///     Gets the measurements captured so far.
    /// </summary>
    /// <returns>A copy of the captured snapshot metric measurements.</returns>
    public List<SnapshotMetricMeasurement> Snapshot()
    {
        lock (syncLock)
        {
            return [.. measurements];
        }
    }

    private void Record(
        string instrumentName,
        long value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags
    )
    {
        Dictionary<string, object?> tagMap = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> tag in tags)
        {
            tagMap[tag.Key] = tag.Value;
        }

        lock (syncLock)
        {
            measurements.Add(new(instrumentName, value, tagMap));
        }
    }

    private void Record(
        string instrumentName,
        int value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags
    ) =>
        Record(instrumentName, (long)value, tags);
}