using System.Diagnostics;


namespace Mississippi.Observability;

/// <summary>
///     Provides the ActivitySource for snapshot operations.
/// </summary>
public static class SnapshotsTelemetry
{
    /// <summary>
    ///     ActivitySource for tracing snapshot operations.
    /// </summary>
    public static readonly ActivitySource Source = new(
        MississippiActivitySources.Snapshots,
        MississippiActivitySources.Version);
}
