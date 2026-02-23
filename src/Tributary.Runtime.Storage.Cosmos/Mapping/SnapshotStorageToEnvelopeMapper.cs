using System.Collections.Immutable;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.Mapping;

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