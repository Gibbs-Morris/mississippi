using System;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Handlers;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Reducers;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Registrations;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;

using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.PinnedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Controllers.Aggregates.Commands.SavedItems;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.ReferenceSet;

/// <summary>Proves declared consumer commands compile, generate and execute through actual registrations.</summary>
public sealed class ReferenceSetConsumerTests
{
    /// <summary>Models a snapshot shape that cannot satisfy the cache construction requirement.</summary>
    /// <param name="Name">The value required by its only constructor.</param>
    private sealed record ConstructorOnlyAggregate(string Name);

    /// <summary>Generated mappings preserve inputs and the same behavior supports independent named collections.</summary>
    [Fact]
    public void GeneratedCommandsExecuteForTwoNamedCollections()
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        Assert.IsType<AddReferenceHandler<AddCatalogItem, SavedItemsAggregate, CatalogItemProjection>>(
            provider.GetRequiredService<ICommandHandler<AddCatalogItem, SavedItemsAggregate>>());
        Assert.IsType<RemoveReferenceHandler<RemoveCatalogItem, SavedItemsAggregate, CatalogItemProjection>>(
            provider.GetRequiredService<ICommandHandler<RemoveCatalogItem, SavedItemsAggregate>>());
        Assert.IsType<ReferenceAddedReducer<SavedItemsAggregate, CatalogItemProjection>>(
            provider.GetRequiredService<IEventReducer<ReferenceAdded, SavedItemsAggregate>>());
        Assert.IsType<ReferenceRemovedReducer<SavedItemsAggregate, CatalogItemProjection>>(
            provider.GetRequiredService<IEventReducer<ReferenceRemoved, SavedItemsAggregate>>());
        AddCatalogItem command = provider.GetRequiredService<IMapper<AddCatalogItemDto, AddCatalogItem>>()
            .Map(
                new()
                {
                    SourceId = " Catalog/A ",
                    ObservedSourceVersion = long.MaxValue,
                });
        Assert.Equal(" Catalog/A ", command.SourceId);
        Assert.Equal(long.MaxValue, command.ObservedSourceVersion);
        IRootCommandHandler<SavedItemsAggregate> saved =
            provider.GetRequiredService<IRootCommandHandler<SavedItemsAggregate>>();
        IRootCommandHandler<PinnedItemsAggregate> pinned =
            provider.GetRequiredService<IRootCommandHandler<PinnedItemsAggregate>>();
        object savedEvent = Assert.Single(saved.Handle(command, null).Value!);
        object pinnedEvent = Assert.Single(pinned.Handle(command, null).Value!);
        SavedItemsAggregate savedState =
            provider.GetRequiredService<IRootReducer<SavedItemsAggregate>>().Reduce(new(), savedEvent);
        PinnedItemsAggregate pinnedState = provider.GetRequiredService<IRootReducer<PinnedItemsAggregate>>()
            .Reduce(new(), pinnedEvent);
        Assert.Equal(command.ObservedSourceVersion, savedState.References.Members[command.SourceId]);
        Assert.Equal(command.ObservedSourceVersion, pinnedState.References.Members[command.SourceId]);
        RemoveCatalogItem removal = provider.GetRequiredService<IMapper<RemoveCatalogItemDto, RemoveCatalogItem>>()
            .Map(
                new()
                {
                    SourceId = command.SourceId,
                });
        Assert.Equal(command.SourceId, removal.SourceId);
        SavedItemsAggregate removed = provider.GetRequiredService<IRootReducer<SavedItemsAggregate>>()
            .Reduce(savedState, Assert.Single(saved.Handle(removal, savedState).Value!));
        Assert.Empty(removed.References.Members);
        Assert.Single(savedState.References.Members);
        Assert.Single(pinnedState.References.Members);
    }

    /// <summary>Rejects collection types that the normal snapshot hydration path cannot construct.</summary>
    [Fact]
    public void RegistrationRejectsSnapshotTypesWithoutPublicParameterlessConstructor()
    {
        Type snapshotCache = typeof(SnapshotRegistrations).Assembly.GetTypes()
            .Single(type => type.IsGenericTypeDefinition &&
                            type.GetInterfaces()
                                .Any(contract =>
                                    contract.IsGenericType &&
                                    (contract.GetGenericTypeDefinition() == typeof(ISnapshotCacheGrain<>))));
        Assert.Throws<ArgumentException>(() => snapshotCache.MakeGenericType(typeof(ConstructorOnlyAggregate)));
        MethodInfo registration = Assert.IsType<MethodInfo>(
            typeof(ReferenceSetRegistrations).GetMethod(nameof(ReferenceSetRegistrations.AddReferenceSet)),
            false);
        Assert.Throws<ArgumentException>(() => registration.MakeGenericMethod(
            typeof(ConstructorOnlyAggregate),
            typeof(CatalogItemProjection),
            typeof(AddCatalogItem),
            typeof(RemoveCatalogItem)));
    }
}