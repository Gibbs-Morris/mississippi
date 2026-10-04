using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace MississippiSamples.Spring.Domain.Projections.AuthProof;

/// <summary>
///     Read projection used to prove generated projection endpoint authorization behavior.
/// </summary>
[ProjectionPath("auth-proof")]
[BrookName("SPRING", "AUTHPROOF", "FLOW")]
[SnapshotStorageName("SPRING", "AUTHPROOF", "FLOWPROJECTION")]
[GenerateProjectionEndpoints]
[GenerateAuthorization(Policy = "spring.auth-proof.claim")]
[GenerateSerializer]
[Alias("MississippiSamples.Spring.Domain.Projections.AuthProof.AuthProofProjection")]
public sealed record AuthProofProjection
{
    /// <summary>
    ///     Gets the count of authenticated-access events observed by this projection.
    /// </summary>
    [Id(0)]
    public int AuthenticatedAccessCount { get; init; }
}