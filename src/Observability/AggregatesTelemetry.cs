using System.Diagnostics;


namespace Mississippi.Observability;

/// <summary>
///     Provides the ActivitySource for aggregate operations.
/// </summary>
public static class AggregatesTelemetry
{
    /// <summary>
    ///     ActivitySource for tracing aggregate operations.
    /// </summary>
    public static readonly ActivitySource Source = new(
        MississippiActivitySources.Aggregates,
        MississippiActivitySources.Version);
}
