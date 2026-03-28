using System;
using System.Collections.Immutable;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Azure.Storage
{
    /// <summary>
    ///     Serializes Tributary snapshots to and from Azure Blob snapshot documents.
    /// </summary>
    internal sealed class AzureSnapshotDocumentCodec : ISnapshotDocumentCodec
    {
        private static int CurrentSchemaVersion => 1;

        /// <summary>
        ///     Decodes an Azure Blob payload into a Tributary snapshot envelope.
        /// </summary>
        /// <param name="payload">The encoded Azure Blob payload.</param>
        /// <returns>The decoded Tributary snapshot envelope.</returns>
        public SnapshotEnvelope Decode(
            BinaryData payload
        )
        {
            ArgumentNullException.ThrowIfNull(payload);

            AzureSnapshotDocument document = payload.ToObjectFromJson<AzureSnapshotDocument>() ?? throw new InvalidOperationException(
                "Tributary Azure snapshot blob payload was empty.");

            return new SnapshotEnvelope
            {
                Data = ImmutableArray.Create(document.Data),
                DataContentType = document.DataContentType,
                DataSizeBytes = document.DataSizeBytes,
                ReducerHash = document.ReducerHash,
            };
        }

        /// <summary>
        ///     Encodes a Tributary snapshot envelope for Azure Blob storage.
        /// </summary>
        /// <param name="snapshotKey">The key that identifies the snapshot.</param>
        /// <param name="snapshot">The snapshot payload to encode.</param>
        /// <returns>The encoded Azure Blob payload.</returns>
        public BinaryData Encode(
            SnapshotKey snapshotKey,
            SnapshotEnvelope snapshot
        )
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            AzureSnapshotDocument document = new()
            {
                StreamKey = snapshotKey.Stream.ToString(),
                Version = snapshotKey.Version,
                ReducerHash = snapshot.ReducerHash,
                DataContentType = snapshot.DataContentType,
                Data = [.. snapshot.Data],
                DataSizeBytes = snapshot.DataSizeBytes > 0 ? snapshot.DataSizeBytes : snapshot.Data.Length,
                SchemaVersion = CurrentSchemaVersion,
            };

            return BinaryData.FromObjectAsJson(document);
        }
    }
}
