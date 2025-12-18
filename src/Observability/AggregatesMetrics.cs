using System.Diagnostics.Metrics;


namespace Mississippi.Observability;

/// <summary>
///     Provides metrics instrumentation for aggregate operations.
/// </summary>
public static class AggregatesMetrics
{
    /// <summary>
    ///     Meter for aggregate operation metrics.
    /// </summary>
    public static readonly Meter Meter = new(
        MississippiMeters.Aggregates,
        MississippiMeters.Version);

    /// <summary>
    ///     Counter for commands executed.
    /// </summary>
    public static readonly Counter<long> CommandsExecuted = Meter.CreateCounter<long>(
        "mississippi.aggregates.commands_executed_total",
        unit: "{command}",
        description: "Total number of commands executed against aggregates");

    /// <summary>
    ///     Histogram for command execution duration.
    /// </summary>
    public static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
        "mississippi.aggregates.command_duration_ms",
        unit: "ms",
        description: "Duration of command execution in milliseconds");

    /// <summary>
    ///     Counter for command failures.
    /// </summary>
    public static readonly Counter<long> CommandFailures = Meter.CreateCounter<long>(
        "mississippi.aggregates.command_failures_total",
        unit: "{command}",
        description: "Total number of failed command executions");
}
