using System.Collections.Generic;


namespace Mississippi.Observability;

/// <summary>
///     Central registry of all ActivitySource instances used by Mississippi components.
///     Provides a single location to discover and subscribe to framework tracing.
/// </summary>
public static class MississippiActivitySources
{
    /// <summary>
    ///     ActivitySource name for aggregate operations.
    /// </summary>
    public const string Aggregates = "Mississippi.Aggregates";

    /// <summary>
    ///     ActivitySource name for brook storage operations.
    /// </summary>
    public const string Brooks = "Mississippi.Brooks";

    /// <summary>
    ///     ActivitySource name for reducer operations.
    /// </summary>
    public const string Reducers = "Mississippi.Reducers";

    /// <summary>
    ///     ActivitySource name for snapshot operations.
    /// </summary>
    public const string Snapshots = "Mississippi.Snapshots";

    /// <summary>
    ///     ActivitySource name for serialization operations.
    /// </summary>
    public const string Serialization = "Mississippi.Serialization";

    /// <summary>
    ///     Gets all ActivitySource names used by the Mississippi framework.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Aggregates,
        Brooks,
        Reducers,
        Snapshots,
        Serialization
    ];

    /// <summary>
    ///     The version of the telemetry instrumentation.
    /// </summary>
    internal const string Version = "1.0.0";
}
