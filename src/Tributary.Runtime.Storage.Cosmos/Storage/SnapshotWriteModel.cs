using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Combines the snapshot key and envelope for mapping to a storage document.
/// </summary>
internal sealed record SnapshotWriteModel(SnapshotKey Key, SnapshotEnvelope Snapshot);