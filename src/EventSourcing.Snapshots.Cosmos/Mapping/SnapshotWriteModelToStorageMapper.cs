using System.Linq;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Mapping;

/// <summary>
///     Maps snapshot write models to storage models.
/// </summary>
internal sealed class SnapshotWriteModelToStorageMapper : IMapper<SnapshotWriteModel, SnapshotStorageModel>
{
    /// <inheritdoc />
    public SnapshotStorageModel Map(
        SnapshotWriteModel input
    ) =>
        new()
        {
            StreamKey = input.Key.Stream,
            Version = input.Key.Version,
            Data = input.Snapshot.Data.ToArray(),
            DataContentType = input.Snapshot.DataContentType,
        };
}