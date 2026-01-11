using System.Globalization;
using System.Linq;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Mapping;

/// <summary>
///     Maps snapshot write models to Cosmos snapshot documents.
/// </summary>
internal sealed class SnapshotWriteModelToDocumentMapper : IMapper<SnapshotWriteModel, SnapshotDocument>
{
    /// <inheritdoc />
    public SnapshotDocument Map(
        SnapshotWriteModel input
    ) =>
        new()
        {
            Id = input.Key.Version.ToString(CultureInfo.InvariantCulture),
            Type = "snapshot",
            SnapshotPartitionKey = input.Key.Stream.ToString(),
            ProjectionType = input.Key.Stream.SnapshotStorageName,
            ProjectionId = input.Key.Stream.EntityId,
            ReducersHash = input.Key.Stream.ReducersHash,
            Version = input.Key.Version,
            Data = input.Snapshot.Data.ToArray(),
            DataContentType = input.Snapshot.DataContentType,
        };
}