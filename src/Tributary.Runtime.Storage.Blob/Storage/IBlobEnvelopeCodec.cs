using System;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Encodes and decodes the provider-owned Blob frame for snapshot payload storage.
/// </summary>
internal interface IBlobEnvelopeCodec
{
    /// <summary>
    ///     Encodes the supplied snapshot into the stored Blob frame.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key being persisted.</param>
    /// <param name="snapshot">The snapshot payload to encode.</param>
    /// <returns>The encoded Blob frame bytes.</returns>
    byte[] Encode(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot
    );

    /// <summary>
    ///     Decodes the supplied Blob frame for the expected snapshot key.
    /// </summary>
    /// <param name="snapshotKey">The expected snapshot key.</param>
    /// <param name="storedFrame">The stored Blob frame bytes.</param>
    /// <returns>The decoded frame and snapshot payload.</returns>
    DecodedSnapshotBlobFrame Decode(
        SnapshotKey snapshotKey,
        ReadOnlyMemory<byte> storedFrame
    );
}