using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

/// <summary>Provides a concrete source projection type for membership metadata.</summary>
[BrookName("TEST", "CATALOG", "ITEM")]
[SnapshotStorageName("TEST", "CATALOG", "ITEM")]
[GenerateSerializer]
[Alias("MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem.CatalogItemProjection")]
internal sealed record CatalogItemProjection
{
    /// <summary>Gets source content that is never copied into membership.</summary>
    [Id(0)]
    public string Title { get; init; } = string.Empty;
}