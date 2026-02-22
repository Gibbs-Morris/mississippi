using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

/// <summary>
///     Combines the snapshot key and envelope for mapping to a storage document.
/// </summary>
internal sealed record SnapshotWriteModel(SnapshotKey Key, SnapshotEnvelope Snapshot);