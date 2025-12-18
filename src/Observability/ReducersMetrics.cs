using System.Diagnostics.Metrics;


namespace Mississippi.Observability;

/// <summary>
///     Provides metrics instrumentation for reducer operations.
/// </summary>
public static class ReducersMetrics
{
    /// <summary>
    ///     Meter for reducer operation metrics.
    /// </summary>
    public static readonly Meter Meter = new(
        MississippiMeters.Reducers,
        MississippiMeters.Version);

    /// <summary>
    ///     Counter for events reduced.
    /// </summary>
    public static readonly Counter<long> EventsReduced = Meter.CreateCounter<long>(
        "mississippi.reducers.events_reduced_total",
        unit: "{event}",
        description: "Total number of events reduced");

    /// <summary>
    ///     Histogram for reduction duration.
    /// </summary>
    public static readonly Histogram<double> ReduceDuration = Meter.CreateHistogram<double>(
        "mississippi.reducers.reduce_duration_ms",
        unit: "ms",
        description: "Duration of reduction operations in milliseconds");
}
