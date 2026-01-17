namespace Mississippi.EventSourcing.Snapshots.Cosmos;

/// <summary>
///     Represents a snapshot identifier paired with its version number.
/// </summary>
/// <param name="Id">The document identifier.</param>
/// <param name="Version">The snapshot version.</param>
internal sealed record SnapshotIdVersion(string Id, long Version);