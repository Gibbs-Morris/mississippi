using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.Mapping;

/// <summary>
///     Maps Cosmos snapshot documents to storage models.
/// </summary>
internal sealed class SnapshotDocumentToStorageMapper : IMapper<SnapshotDocument, SnapshotStorageModel>
{
    /// <inheritdoc />
    public SnapshotStorageModel Map(
        SnapshotDocument input
    ) =>
        new()
        {
            StreamKey = new(input.BrookName, input.ProjectionType, input.ProjectionId, input.ReducersHash),
            Version = input.Version,
            Data = input.Data,
            DataSizeBytes = input.DataSizeBytes,
            DataContentType = input.DataContentType,
        };
}