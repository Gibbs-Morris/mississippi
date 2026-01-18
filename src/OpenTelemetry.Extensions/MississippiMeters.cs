using System.Collections.Generic;


namespace Mississippi.OpenTelemetry.Extensions;

/// <summary>
///     Meter names for Mississippi framework components.
///     Use with <see cref="MeterBuilderExtensions.AddMississippiMeters" /> to register all meters,
///     or add individual meters using <c>.AddMeter(MississippiMeters.Brooks)</c>.
/// </summary>
public static class MississippiMeters
{
    /// <summary>
    ///     Meter name for Mississippi Aqueduct SignalR backplane metrics.
    /// </summary>
    /// <value>The value is <c>"Mississippi.Aqueduct"</c>.</value>
    public const string Aqueduct = "Mississippi.Aqueduct";

    /// <summary>
    ///     Meter name for Mississippi aggregate command execution metrics.
    /// </summary>
    /// <value>The value is <c>"Mississippi.EventSourcing.Aggregates"</c>.</value>
    public const string Aggregates = "Mississippi.EventSourcing.Aggregates";

    /// <summary>
    ///     Meter name for Mississippi brook (event stream) read/write metrics.
    /// </summary>
    /// <value>The value is <c>"Mississippi.EventSourcing.Brooks"</c>.</value>
    public const string Brooks = "Mississippi.EventSourcing.Brooks";

    /// <summary>
    ///     Meter name for Mississippi Inlet real-time notification metrics.
    /// </summary>
    /// <value>The value is <c>"Mississippi.Inlet"</c>.</value>
    public const string Inlet = "Mississippi.Inlet";

    /// <summary>
    ///     Meter name for Mississippi snapshot caching and persistence metrics.
    /// </summary>
    /// <value>The value is <c>"Mississippi.EventSourcing.Snapshots"</c>.</value>
    public const string Snapshots = "Mississippi.EventSourcing.Snapshots";

    /// <summary>
    ///     Meter name for Mississippi distributed lock metrics.
    /// </summary>
    /// <value>The value is <c>"Mississippi.Storage.Locking"</c>.</value>
    public const string StorageLocking = "Mississippi.Storage.Locking";

    /// <summary>
    ///     Meter name for Mississippi Cosmos DB snapshot storage metrics.
    /// </summary>
    /// <value>The value is <c>"Mississippi.Storage.Snapshots"</c>.</value>
    public const string StorageSnapshots = "Mississippi.Storage.Snapshots";

    /// <summary>
    ///     Meter name for Mississippi UX projection query and subscription metrics.
    /// </summary>
    /// <value>The value is <c>"Mississippi.EventSourcing.UxProjections"</c>.</value>
    public const string UxProjections = "Mississippi.EventSourcing.UxProjections";

    /// <summary>
    ///     Gets all Mississippi meter names.
    /// </summary>
    /// <returns>An array containing all meter names.</returns>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        Aqueduct,
        Aggregates,
        Brooks,
        Inlet,
        Snapshots,
        StorageLocking,
        StorageSnapshots,
        UxProjections,
    };
}
