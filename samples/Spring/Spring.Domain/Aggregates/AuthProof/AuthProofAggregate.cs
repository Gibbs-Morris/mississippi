using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Mississippi.Spring.Domain.Aggregates.AuthProof;

/// <summary>
///     Aggregate used to prove generated endpoint authorization behaviors in local development.
/// </summary>
[BrookName("SPRING", "AUTHPROOF", "FLOW")]
[SnapshotStorageName("SPRING", "AUTHPROOF", "FLOWSTATE")]
[GenerateAggregateEndpoints]
[GenerateSerializer]
[Alias("Spring.Domain.Aggregates.AuthProof.AuthProofAggregate")]
public sealed record AuthProofAggregate
{
    /// <summary>
    ///     Gets the count of successfully authorized authenticated-only calls.
    /// </summary>
    [Id(0)]
    public int AuthenticatedAccessCount { get; init; }

    /// <summary>
    ///     Gets the count of successfully authorized claim-policy calls.
    /// </summary>
    [Id(1)]
    public int ClaimPolicyAccessCount { get; init; }

    /// <summary>
    ///     Gets the count of successfully authorized role-protected calls.
    /// </summary>
    [Id(2)]
    public int RoleAccessCount { get; init; }
}