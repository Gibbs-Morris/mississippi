using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Projections.PinnedItemsMembership;

/// <summary>Provides a named read-only membership view over its collection brook.</summary>
[BrookName("TEST", "REFERENCESET", "PINNEDITEMS")]

// ReSharper disable once RedundantArgumentDefaultValue
[SnapshotStorageName("TEST", "REFERENCESET", "PINNEDITEMSMEMBERSHIP", 1)]
[GenerateSerializer]
[Alias(
    "MississippiTests.DomainModeling.Runtime.L0Tests.Projections.PinnedItemsMembership.PinnedItemsMembershipProjection")]
internal sealed record PinnedItemsMembershipProjection
{
    /// <summary>Gets source-typed membership without source content.</summary>
    [Id(0)]
    public ReferenceSetState<CatalogItemProjection> References { get; init; } = new();
}