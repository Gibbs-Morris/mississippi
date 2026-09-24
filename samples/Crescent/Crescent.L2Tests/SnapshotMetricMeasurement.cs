namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Represents a single measurement captured from a snapshot metric instrument.
/// </summary>
/// <param name="InstrumentName">The metric instrument name.</param>
/// <param name="Value">The numeric measurement value.</param>
/// <param name="Tags">The measurement tags.</param>
internal sealed record SnapshotMetricMeasurement(
    string InstrumentName,
    long Value,
    IReadOnlyDictionary<string, object?> Tags
);