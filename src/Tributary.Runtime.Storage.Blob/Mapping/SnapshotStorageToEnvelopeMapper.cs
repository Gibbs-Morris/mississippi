using System.Collections.Immutable;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Mapping;

/// <summary>
///     Maps Blob storage models to snapshot envelopes.
/// </summary>
internal sealed class SnapshotStorageToEnvelopeMapper : IMapper<SnapshotBlobStorageModel, SnapshotEnvelope>
{
    /// <inheritdoc />
    public SnapshotEnvelope Map(
        SnapshotBlobStorageModel input
    ) =>
        new()
        {
            Data = input.Data.ToImmutableArray(),
            DataSizeBytes = input.DataSizeBytes,
            DataContentType = input.DataContentType,
            ReducerHash = input.StreamKey.ReducersHash,
        };
}
