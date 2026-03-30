using System;

using Mississippi.Common.Abstractions.Mapping;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests.Fixtures;

/// <summary>
///     Maps the sample projection to the sample replica contract.
/// </summary>
internal sealed class MappedReplicaProjectionToContractMapper : IMapper<MappedReplicaProjection, MappedReplicaContract>
{
    /// <inheritdoc />
    public MappedReplicaContract Map(
        MappedReplicaProjection input
    )
    {
        ArgumentNullException.ThrowIfNull(input);
        return new()
        {
            Id = input.Id,
        };
    }
}