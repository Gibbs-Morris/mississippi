using System.Diagnostics;


namespace Mississippi.Observability;

/// <summary>
///     Provides the ActivitySource for brook storage operations.
/// </summary>
public static class BrooksTelemetry
{
    /// <summary>
    ///     ActivitySource for tracing brook storage operations.
    /// </summary>
    public static readonly ActivitySource Source = new(
        MississippiActivitySources.Brooks,
        MississippiActivitySources.Version);
}
