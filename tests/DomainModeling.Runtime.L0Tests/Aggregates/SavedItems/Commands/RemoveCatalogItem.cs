using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.Commands;
using Mississippi.Inlet.Generators.Abstractions;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands;

/// <summary>Removes a source reference through normal command generation.</summary>
/// <param name="SourceId">The exact source identifier.</param>
[GenerateCommand]
[GenerateSerializer]
[Alias("MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands.RemoveCatalogItem")]
internal sealed record RemoveCatalogItem([property: Id(0)] string SourceId) : IRemoveReference<CatalogItemProjection>;