using System.Collections.Immutable;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Mapping;

/// <summary>
///     Maps storage models to snapshot envelopes.
/// </summary>
internal sealed class SnapshotStorageToEnvelopeMapper : IMapper<SnapshotStorageModel, SnapshotEnvelope>
{
    /// <inheritdoc />
    public SnapshotEnvelope Map(
        SnapshotStorageModel input
    ) =>
        new()
        {
            Data = input.Data.ToImmutableArray(),
            DataSizeBytes = input.DataSizeBytes,
            DataContentType = input.DataContentType,
        };
}