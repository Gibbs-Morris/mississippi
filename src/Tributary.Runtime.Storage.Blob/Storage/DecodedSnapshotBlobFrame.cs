using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Represents a decoded stored Blob frame and reconstructed snapshot payload.
/// </summary>
/// <param name="Header">The decoded stored header.</param>
/// <param name="Snapshot">The reconstructed snapshot envelope.</param>
/// <param name="Compression">The resolved compression mode used by the payload segment.</param>
internal sealed record DecodedSnapshotBlobFrame(
    StoredSnapshotBlobHeader Header,
    SnapshotEnvelope Snapshot,
    SnapshotBlobCompression Compression
);