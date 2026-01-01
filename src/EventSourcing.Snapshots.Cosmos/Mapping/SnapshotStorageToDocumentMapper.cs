using System.Globalization;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Mapping;

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
            ProjectionType = input.StreamKey.ProjectionType,
            ProjectionId = input.StreamKey.ProjectionId,
            ReducersHash = input.StreamKey.ReducersHash,
            Version = input.Version,
            Data = input.Data,
            DataContentType = input.DataContentType,
        };
}