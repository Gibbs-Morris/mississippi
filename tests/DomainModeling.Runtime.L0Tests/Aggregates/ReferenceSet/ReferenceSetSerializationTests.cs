using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.Tributary.Abstractions;

using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

using Orleans.Serialization;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.ReferenceSet;

/// <summary>Proves real event and snapshot codecs preserve membership facts and old immutable states.</summary>
public sealed class ReferenceSetSerializationTests
{
    /// <summary>JSON storage and Orleans transport rebuild equivalent ordinal membership after remove/re-add.</summary>
    [Fact]
    public void RealCodecsAndReplayPreserveFacts()
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        IRootCommandHandler<SavedItemsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<SavedItemsAggregate>>();
        IRootReducer<SavedItemsAggregate> reducer = provider.GetRequiredService<IRootReducer<SavedItemsAggregate>>();
        IBrookEventConverter events = provider.GetRequiredService<IBrookEventConverter>();
        ISnapshotStateConverter<SavedItemsAggregate> snapshots =
            provider.GetRequiredService<ISnapshotStateConverter<SavedItemsAggregate>>();
        Serializer serializer = provider.GetRequiredService<Serializer>();
        List<SavedItemsAggregate> history = [new()];
        SavedItemsAggregate replay = new();
        foreach (object command in new object[]
                 {
                     new AddCatalogItem(" A ", long.MaxValue),
                     new AddCatalogItem("a", 0),
                     new RemoveCatalogItem(" A "),
                     new AddCatalogItem(" A ", 9),
                 })
        {
            object fact = Assert.Single(handler.Handle(command, history[^1]).Value!);
            BrookEvent stored = Assert.Single(
                events.ToStorageEvents(BrookKey.ForType<SavedItemsAggregate>("collection"), [fact]));
            Assert.Equal(
                fact is ReferenceAdded ? "MISSISSIPPI.REFERENCESET.ADDED.V1" : "MISSISSIPPI.REFERENCESET.REMOVED.V1",
                stored.EventType);
            object restored = events.ToDomainEvent(stored);
            Assert.Equal(fact, restored);
            Assert.Equal(fact, serializer.Deserialize<object>(serializer.SerializeToArray(fact)));
            history.Add(reducer.Reduce(history[^1], fact));
            replay = reducer.Reduce(replay, restored);
        }

        SavedItemsAggregate expected = history[^1];
        SnapshotEnvelope envelope = snapshots.ToEnvelope(replay, reducer.GetReducerHash());
        SavedItemsAggregate fromJson = snapshots.FromEnvelope(envelope);
        SavedItemsAggregate fromOrleans = Assert.IsType<SavedItemsAggregate>(
            serializer.Deserialize<SavedItemsAggregate>(serializer.SerializeToArray(expected)));
        foreach (SavedItemsAggregate restored in new[] { replay, fromJson, fromOrleans })
        {
            Assert.Equal(
                expected.References.Members.OrderBy(pair => pair.Key, StringComparer.Ordinal),
                restored.References.Members.OrderBy(pair => pair.Key, StringComparer.Ordinal));
            Assert.Equal(9, restored.References.Members[" A "]);
            Assert.Equal(0, restored.References.Members["a"]);
            Assert.Equal(typeof(CatalogItemProjection), restored.References.SourceType);
        }

        Assert.DoesNotContain("SourceType", Encoding.UTF8.GetString(envelope.Data.AsSpan()), StringComparison.Ordinal);
        Assert.DoesNotContain("Title", Encoding.UTF8.GetString(envelope.Data.AsSpan()), StringComparison.Ordinal);
        Assert.Empty(history[0].References.Members);
        Assert.Equal(long.MaxValue, history[1].References.Members[" A "]);
        Assert.Single(history[1].References.Members);
        Assert.Equal(2, history[2].References.Members.Count);
        Assert.False(history[3].References.Members.ContainsKey(" A "));
        Assert.Single(history[3].References.Members);
    }
}