using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.Commands;
using Mississippi.Inlet.Generators.Abstractions;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands;

/// <summary>Adds a source reference through normal command generation.</summary>
/// <param name="SourceId">The exact source identifier.</param>
/// <param name="ObservedSourceVersion">The nonnegative observed source version.</param>
[GenerateCommand]
[GenerateSerializer]
[Alias("MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands.AddCatalogItem")]
internal sealed record AddCatalogItem([property: Id(0)] string SourceId, [property: Id(1)] long ObservedSourceVersion)
    : IAddReference<CatalogItemProjection>;