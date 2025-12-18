using System.Diagnostics;


namespace Mississippi.Observability;

/// <summary>
///     Provides the ActivitySource for reducer operations.
/// </summary>
public static class ReducersTelemetry
{
    /// <summary>
    ///     ActivitySource for tracing reducer operations.
    /// </summary>
    public static readonly ActivitySource Source = new(
        MississippiActivitySources.Reducers,
        MississippiActivitySources.Version);
}
