using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.EventSourcing.Cosmos.Abstractions;

internal interface IStreamRecoveryService
{
    Task<BrookPosition> GetOrRecoverHeadPositionAsync(BrookKey brookId, CancellationToken cancellationToken = default);
}