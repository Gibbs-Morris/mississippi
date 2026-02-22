using System.Collections.Immutable;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.Mapping;

/// <summary>
///     Maps Cosmos snapshot documents directly to envelopes (used by tests or specialized flows).
/// </summary>
internal sealed class SnapshotDocumentToEnvelopeMapper : IMapper<SnapshotDocument, SnapshotEnvelope>
{
    /// <inheritdoc />
    public SnapshotEnvelope Map(
        SnapshotDocument input
    ) =>
        new()
        {
            Data = input.Data.ToImmutableArray(),
            DataSizeBytes = input.DataSizeBytes,
            DataContentType = input.DataContentType,
        };
}