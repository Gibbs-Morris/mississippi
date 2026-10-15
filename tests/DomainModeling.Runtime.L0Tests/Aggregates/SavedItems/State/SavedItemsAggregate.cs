using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.State;

/// <summary>Provides a named consumer snapshot for reference-set verification.</summary>
[BrookName("TEST", "REFERENCESET", "SAVEDITEMS")]
[SnapshotStorageName("TEST", "REFERENCESET", "SAVEDITEMS")]
[GenerateSerializer]
[Alias("MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.State.SavedItemsAggregate")]
internal sealed record SavedItemsAggregate
{
    /// <summary>Gets source-typed membership.</summary>
    [Id(0)]
    public ReferenceSetState<CatalogItemProjection> References { get; init; } = new();
}