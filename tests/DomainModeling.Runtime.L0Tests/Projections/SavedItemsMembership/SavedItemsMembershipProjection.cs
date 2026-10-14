using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Projections.SavedItemsMembership;

/// <summary>Provides a named read-only membership view over its collection brook.</summary>
[BrookName("TEST", "REFERENCESET", "SAVEDITEMS")]
[SnapshotStorageName("TEST", "REFERENCESET", "SAVEDITEMSMEMBERSHIP")]
[GenerateSerializer]
[Alias(
    "MississippiTests.DomainModeling.Runtime.L0Tests.Projections.SavedItemsMembership.SavedItemsMembershipProjection")]
internal sealed record SavedItemsMembershipProjection
{
    /// <summary>Gets source-typed membership without source content.</summary>
    [Id(0)]
    public ReferenceSetState<CatalogItemProjection> References { get; init; } = new();
}