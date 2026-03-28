using System;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Azure.Storage
{
    /// <summary>
    ///     Serializes Tributary snapshot documents to and from Azure Blob payloads.
    /// </summary>
    internal interface ISnapshotDocumentCodec
    {
        /// <summary>
        ///     Decodes an Azure Blob payload into a snapshot envelope.
        /// </summary>
        /// <param name="payload">The Azure Blob payload.</param>
        /// <returns>The decoded snapshot envelope.</returns>
        SnapshotEnvelope Decode(BinaryData payload);

        /// <summary>
        ///     Encodes a snapshot envelope for Azure Blob storage.
        /// </summary>
        /// <param name="snapshotKey">The snapshot key being encoded.</param>
        /// <param name="snapshot">The snapshot envelope.</param>
        /// <returns>The encoded Azure Blob payload.</returns>
        BinaryData Encode(
            SnapshotKey snapshotKey,
            SnapshotEnvelope snapshot
        );
    }
}
