using System.Diagnostics.Metrics;


namespace Mississippi.Observability;

/// <summary>
///     Provides metrics instrumentation for brook storage operations.
/// </summary>
public static class BrooksMetrics
{
    /// <summary>
    ///     Meter for brook storage operation metrics.
    /// </summary>
    public static readonly Meter Meter = new(
        MississippiMeters.Brooks,
        MississippiMeters.Version);

    /// <summary>
    ///     Counter for events appended.
    /// </summary>
    public static readonly Counter<long> EventsAppended = Meter.CreateCounter<long>(
        "mississippi.brooks.events_appended_total",
        unit: "{event}",
        description: "Total number of events appended to brooks");

    /// <summary>
    ///     Counter for events read.
    /// </summary>
    public static readonly Counter<long> EventsRead = Meter.CreateCounter<long>(
        "mississippi.brooks.events_read_total",
        unit: "{event}",
        description: "Total number of events read from brooks");

    /// <summary>
    ///     Histogram for event append duration.
    /// </summary>
    public static readonly Histogram<double> AppendDuration = Meter.CreateHistogram<double>(
        "mississippi.brooks.append_duration_ms",
        unit: "ms",
        description: "Duration of event append operations in milliseconds");

    /// <summary>
    ///     Histogram for event read duration.
    /// </summary>
    public static readonly Histogram<double> ReadDuration = Meter.CreateHistogram<double>(
        "mississippi.brooks.read_duration_ms",
        unit: "ms",
        description: "Duration of event read operations in milliseconds");

    /// <summary>
    ///     Counter for append failures.
    /// </summary>
    public static readonly Counter<long> AppendFailures = Meter.CreateCounter<long>(
        "mississippi.brooks.append_failures_total",
        unit: "{operation}",
        description: "Total number of failed event append operations");
}
