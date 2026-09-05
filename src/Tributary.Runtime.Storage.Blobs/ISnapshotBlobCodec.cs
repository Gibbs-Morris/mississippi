using System;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     Converts snapshot envelopes to and from validated Blob documents.
/// </summary>
internal interface ISnapshotBlobCodec
{
    /// <summary>
    ///     Decodes a Blob document for the requested snapshot key.
    /// </summary>
    /// <param name="snapshotKey">The key the persisted document must match.</param>
    /// <param name="document">The serialized Blob document.</param>
    /// <returns>The validated snapshot envelope.</returns>
    SnapshotEnvelope Decode(
        SnapshotKey snapshotKey,
        BinaryData document
    );

    /// <summary>
    ///     Encodes a snapshot envelope as a Blob document.
    /// </summary>
    /// <param name="snapshotKey">The key identifying the snapshot.</param>
    /// <param name="snapshot">The snapshot envelope to persist.</param>
    /// <returns>The validated serialized Blob document.</returns>
    BinaryData Encode(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot
    );
}