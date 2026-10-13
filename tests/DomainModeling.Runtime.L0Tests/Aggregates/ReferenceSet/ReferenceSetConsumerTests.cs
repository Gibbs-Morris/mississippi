using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;

using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.PinnedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Controllers.Aggregates.Commands.SavedItems;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.ReferenceSet;

/// <summary>Proves declared consumer commands compile, generate and execute through actual registrations.</summary>
public sealed class ReferenceSetConsumerTests
{
    /// <summary>Generated mappings preserve inputs and the same behavior supports independent named collections.</summary>
    [Fact]
    public void GeneratedCommandsExecuteForTwoNamedCollections()
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
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
}