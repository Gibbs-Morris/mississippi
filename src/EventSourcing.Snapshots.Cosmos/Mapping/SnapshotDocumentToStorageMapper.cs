using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Mapping;

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
            DataContentType = input.DataContentType,
        };
}