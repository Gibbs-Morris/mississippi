using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Projections.PositionalMembership;

/// <summary>Demonstrates an unsupported consumer without a public parameterless constructor.</summary>
/// <param name="References">The constructor-required membership.</param>
[BrookName("TEST", "REFERENCESET", "SAVEDITEMS")]

// ReSharper disable once RedundantArgumentDefaultValue
[SnapshotStorageName("TEST", "REFERENCESET", "POSITIONALMEMBERSHIP", 1)]
[GenerateSerializer]
[Alias(
    "MississippiTests.DomainModeling.Runtime.L0Tests.Projections.PositionalMembership.PositionalMembershipProjection")]
internal sealed record PositionalMembershipProjection(
    [property: Id(0)] ReferenceSetState<CatalogItemProjection> References
);