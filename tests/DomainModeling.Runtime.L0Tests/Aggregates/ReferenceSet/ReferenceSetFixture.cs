using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Registrations;

using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.PinnedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Controllers.Aggregates.Commands.SavedItems.Mappers;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.PinnedItemsMembership;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.SavedItemsMembership;

using Orleans.Serialization;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.ReferenceSet;

/// <summary>Provides compiled consumer registration for native reference-set tests.</summary>
internal static class ReferenceSetFixture
{
    /// <summary>Builds actual consumer registrations and serializers.</summary>
    /// <returns>The owned service provider.</returns>
    public static ServiceProvider Provider()
    {
        ServiceCollection services = new();
        services.AddReferenceSet<SavedItemsAggregate, CatalogItemProjection, AddCatalogItem, RemoveCatalogItem>(
            state => state?.References ?? new(),
            (state, references) => (state ?? new()) with
            {
                References = references,
            });
        services.AddReferenceSet<PinnedItemsAggregate, CatalogItemProjection, AddCatalogItem, RemoveCatalogItem>(
            state => state?.References ?? new(),
            (state, references) => (state ?? new()) with
            {
                References = references,
            });
        services.AddReferenceSetProjection<SavedItemsMembershipProjection, CatalogItemProjection>(
            state => state?.References ?? new(),
            (state, references) => (state ?? new()) with
            {
                References = references,
            });
        services.AddReferenceSetProjection<PinnedItemsMembershipProjection, CatalogItemProjection>(
            state => state?.References ?? new(),
            (state, references) => (state ?? new()) with
            {
                References = references,
            });
        services.AddSavedItemsAggregateMappers();
        services.AddJsonSerialization();
        services.AddSerializer();
        return services.BuildServiceProvider();
    }
}