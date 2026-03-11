using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Combines the snapshot key and envelope for mapping to Blob storage.
/// </summary>
internal sealed record SnapshotWriteModel(SnapshotKey Key, SnapshotEnvelope Snapshot);
