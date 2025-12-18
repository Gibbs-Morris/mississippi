using System.Diagnostics.Metrics;


namespace Mississippi.Observability;

/// <summary>
///     Provides metrics instrumentation for snapshot operations.
/// </summary>
public static class SnapshotsMetrics
{
    /// <summary>
    ///     Meter for snapshot operation metrics.
    /// </summary>
    public static readonly Meter Meter = new(
        MississippiMeters.Snapshots,
        MississippiMeters.Version);

    /// <summary>
    ///     Counter for snapshot loads.
    /// </summary>
    public static readonly Counter<long> SnapshotLoads = Meter.CreateCounter<long>(
        "mississippi.snapshots.loads_total",
        unit: "{snapshot}",
        description: "Total number of snapshots loaded");

    /// <summary>
    ///     Counter for snapshot saves.
    /// </summary>
    public static readonly Counter<long> SnapshotSaves = Meter.CreateCounter<long>(
        "mississippi.snapshots.saves_total",
        unit: "{snapshot}",
        description: "Total number of snapshots saved");

    /// <summary>
    ///     Histogram for snapshot load duration.
    /// </summary>
    public static readonly Histogram<double> LoadDuration = Meter.CreateHistogram<double>(
        "mississippi.snapshots.load_duration_ms",
        unit: "ms",
        description: "Duration of snapshot load operations in milliseconds");

    /// <summary>
    ///     Histogram for snapshot save duration.
    /// </summary>
    public static readonly Histogram<double> SaveDuration = Meter.CreateHistogram<double>(
        "mississippi.snapshots.save_duration_ms",
        unit: "ms",
        description: "Duration of snapshot save operations in milliseconds");
}
