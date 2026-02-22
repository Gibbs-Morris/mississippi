using System.Globalization;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.Mapping;

/// <summary>
///     Maps storage models to Cosmos snapshot documents.
/// </summary>
internal sealed class SnapshotStorageToDocumentMapper : IMapper<SnapshotStorageModel, SnapshotDocument>
{
    /// <inheritdoc />
    public SnapshotDocument Map(
        SnapshotStorageModel input
    ) =>
        new()
        {
            Id = input.Version.ToString(CultureInfo.InvariantCulture),
            Type = "snapshot",
            SnapshotPartitionKey = input.StreamKey.ToString(),
            BrookName = input.StreamKey.BrookName,
            ProjectionType = input.StreamKey.SnapshotStorageName,
            ProjectionId = input.StreamKey.EntityId,
            ReducersHash = input.StreamKey.ReducersHash,
            Version = input.Version,
            Data = input.Data,
            DataSizeBytes = input.DataSizeBytes,
            DataContentType = input.DataContentType,
        };
}