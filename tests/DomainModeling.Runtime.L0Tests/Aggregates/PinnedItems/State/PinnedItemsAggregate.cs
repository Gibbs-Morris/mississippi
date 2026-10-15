using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.PinnedItems.State;

/// <summary>Provides a named consumer snapshot for reference-set verification.</summary>
[BrookName("TEST", "REFERENCESET", "PINNEDITEMS")]
[SnapshotStorageName("TEST", "REFERENCESET", "PINNEDITEMS")]
[GenerateSerializer]
[Alias("MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.PinnedItems.State.PinnedItemsAggregate")]
internal sealed record PinnedItemsAggregate
{
    /// <summary>Gets source-typed membership.</summary>
    [Id(0)]
    public ReferenceSetState<CatalogItemProjection> References { get; init; } = new();
}