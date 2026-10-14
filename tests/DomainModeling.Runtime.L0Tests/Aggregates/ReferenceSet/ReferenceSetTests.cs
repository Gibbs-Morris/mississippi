using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.Commands;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Registrations;
using Mississippi.Tributary.Abstractions;

using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.ReferenceSet;

/// <summary>Verifies bounded membership decisions and immutable ordinal replay.</summary>
public sealed class ReferenceSetTests
{
    /// <summary>Creates an immutable concrete consumer command with the supplied membership inputs.</summary>
    /// <param name="id">The exact source identifier.</param>
    /// <param name="version">The observed source version.</param>
    /// <returns>The add command.</returns>
    private static AddCatalogItem Add(
        string id,
        long version = 0
    ) =>
        new(id, version);

    /// <summary>Capacity blocks only new valid additions and length uses UTF-16 code units.</summary>
    [Fact]
    public void BoundariesAllowDuplicateAtCapacityAndRejectMalformedInputs()
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        IRootCommandHandler<SavedItemsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<SavedItemsAggregate>>();
        IRootReducer<SavedItemsAggregate> reducer = provider.GetRequiredService<IRootReducer<SavedItemsAggregate>>();
        SavedItemsAggregate state = new();
        for (int index = 0; index < ReferenceSetLimits.MaximumMembers; index++)
        {
            state = reducer.Reduce(
                state,
                Assert.Single(handler.Handle(Add(index.ToString(CultureInfo.InvariantCulture)), state).Value!));
        }

        Assert.Equal(1000, state.References.Members.Count);
        Assert.Empty(handler.Handle(Add("0", long.MaxValue), state).Value!);
        Assert.Equal(0, state.References.Members["0"]);
        Assert.Equal(AggregateErrorCodes.InvalidState, handler.Handle(Add("new"), state).ErrorCode);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, handler.Handle(Add("0", -1), state).ErrorCode);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, handler.Handle(Add(" "), state).ErrorCode);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, handler.Handle(Add(new('x', 257)), state).ErrorCode);
        Assert.Equal(
            AggregateErrorCodes.InvalidCommand,
            handler.Handle(new RemoveCatalogItem(new('x', 257)), state).ErrorCode);
        Assert.Empty(handler.Handle(new RemoveCatalogItem("absent"), state).Value!);
        SavedItemsAggregate smaller = reducer.Reduce(
            state,
            Assert.Single(handler.Handle(new RemoveCatalogItem("0"), state).Value!));
        SavedItemsAggregate replacement = reducer.Reduce(
            smaller,
            Assert.Single(handler.Handle(Add("0", 42), smaller).Value!));
        Assert.Equal(42, replacement.References.Members["0"]);
        Assert.Equal(1000, replacement.References.Members.Count);
        Assert.Equal(1000, state.References.Members.Count);
        string boundaryId = string.Concat(Enumerable.Repeat("\U0001f600", 128));
        Assert.Equal(256, boundaryId.Length);
        Assert.Single(handler.Handle(Add(boundaryId, long.MaxValue), new()).Value!);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, handler.Handle(Add(boundaryId + "x"), new()).ErrorCode);
    }

    /// <summary>Invalid identifiers fail before a no-op, without changing any supplied snapshot.</summary>
    /// <param name="id">The malformed source identifier.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void InvalidIdentifiersFailForAddAndRemove(
        string? id
    )
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        IRootCommandHandler<SavedItemsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<SavedItemsAggregate>>();
        SavedItemsAggregate state = new();
        foreach (object command in new object[] { Add(id!), new RemoveCatalogItem(id!) })
        {
            OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
            Assert.False(result.Success);
            Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
            Assert.Empty(state.References.Members);
        }
    }

    /// <summary>Addition, no-ops, exact identity and remove/re-add retain the agreed loading floors.</summary>
    [Fact]
    public void MembershipTransitionsPreserveOriginalSnapshotsAndLoadingFloors()
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        IRootCommandHandler<SavedItemsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<SavedItemsAggregate>>();
        IRootReducer<SavedItemsAggregate> reducer = provider.GetRequiredService<IRootReducer<SavedItemsAggregate>>();
        SavedItemsAggregate empty = new();
        ReferenceAdded added = Assert.IsType<ReferenceAdded>(Assert.Single(handler.Handle(Add("A", 7), null).Value!));
        Assert.Equal("A", added.SourceId);
        Assert.Equal(7, added.ObservedSourceVersion);
        SavedItemsAggregate first = reducer.Reduce(empty, added);
        Assert.Empty(empty.References.Members);
        Assert.Empty(handler.Handle(Add("A", 99), first).Value!);
        Assert.Empty(handler.Handle(new RemoveCatalogItem("absent"), first).Value!);
        SavedItemsAggregate state = first;
        foreach (string id in new[] { "a", " A ", "\u00e9", "e\u0301" })
        {
            state = reducer.Reduce(state, Assert.Single(handler.Handle(Add(id), state).Value!));
        }

        Assert.Equal(5, state.References.Members.Count);
        ReferenceRemoved removed =
            Assert.IsType<ReferenceRemoved>(Assert.Single(handler.Handle(new RemoveCatalogItem("A"), state).Value!));
        Assert.Equal("A", removed.SourceId);
        SavedItemsAggregate withoutA = reducer.Reduce(state, removed);
        Assert.False(withoutA.References.Members.ContainsKey("A"));
        Assert.True(withoutA.References.Members.ContainsKey("a"));
        SavedItemsAggregate readded = reducer.Reduce(
            withoutA,
            Assert.Single(handler.Handle(Add("A", 12), withoutA).Value!));
        Assert.Equal(12, readded.References.Members["A"]);
        Assert.Equal(7, state.References.Members["A"]);
        Assert.Single(first.References.Members);
    }

    /// <summary>Negative versions fail even for an existing member.</summary>
    /// <param name="version">The negative observed source version.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void NegativeVersionsFailBeforeDuplicateDetection(
        long version
    )
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        IRootCommandHandler<SavedItemsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<SavedItemsAggregate>>();
        SavedItemsAggregate state = new()
        {
            References = new()
            {
                Members = ImmutableDictionary<string, long>.Empty.Add("A", 7),
            },
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(Add("A", version), state);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
        Assert.Equal(7, state.References.Members["A"]);
    }

    /// <summary>Registration rejects null dependencies and preserves existing reducer immutability checks.</summary>
    [Fact]
    public void RegistrationValidatesCallbacksAndReducerImmutability()
    {
        ServiceCollection services = new();
        Func<SavedItemsAggregate?, ReferenceSetState<CatalogItemProjection>> read = state => state?.References ?? new();
        Func<SavedItemsAggregate?, ReferenceSetState<CatalogItemProjection>, SavedItemsAggregate> write =
            (state, references) => (state ?? new()) with
            {
                References = references,
            };
        Assert.Throws<ArgumentNullException>(() => ReferenceSetRegistrations
            .AddReferenceSet<SavedItemsAggregate, CatalogItemProjection, AddCatalogItem, RemoveCatalogItem>(
                null!,
                read,
                write));
        Assert.Throws<ArgumentNullException>(() => services
            .AddReferenceSet<SavedItemsAggregate, CatalogItemProjection, AddCatalogItem, RemoveCatalogItem>(
                null!,
                write));
        Assert.Throws<ArgumentNullException>(() => services
            .AddReferenceSet<SavedItemsAggregate, CatalogItemProjection, AddCatalogItem, RemoveCatalogItem>(
                read,
                null!));
        services.AddReferenceSet<SavedItemsAggregate, CatalogItemProjection, AddCatalogItem, RemoveCatalogItem>(
            read,
            (state, _) => state!);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IRootReducer<SavedItemsAggregate>>()
            .Reduce(
                new(),
                new ReferenceAdded
                {
                    SourceId = "A",
                    ObservedSourceVersion = 0,
                }));
    }

    /// <summary>Replay applies saved facts independently of command capacity and enforces ordinal comparison.</summary>
    [Fact]
    public void ReplayUsesRecordedFactsAndNormalizesMembershipComparer()
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        IRootReducer<SavedItemsAggregate> reducer = provider.GetRequiredService<IRootReducer<SavedItemsAggregate>>();
        SavedItemsAggregate state = new()
        {
            References = new()
            {
                Members = ImmutableDictionary.Create<string, long>(StringComparer.OrdinalIgnoreCase).Add("A", 1),
            },
        };
        IRootCommandHandler<SavedItemsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<SavedItemsAggregate>>();
        SavedItemsAggregate distinct = reducer.Reduce(state, Assert.Single(handler.Handle(Add("a", 2), state).Value!));
        Assert.Equal(2, distinct.References.Members.Count);
        Assert.Equal(1, distinct.References.Members["A"]);
        Assert.Equal(2, distinct.References.Members["a"]);
        Assert.Single(state.References.Members);
        Assert.Equal(typeof(CatalogItemProjection), distinct.References.SourceType);
        Assert.Equal(typeof(CatalogItemProjection), ((IAddReference<CatalogItemProjection>)Add("a")).SourceType);
        Assert.Equal(
            typeof(CatalogItemProjection),
            ((IRemoveReference<CatalogItemProjection>)new RemoveCatalogItem("a")).SourceType);
        SavedItemsAggregate full = new()
        {
            References = new()
            {
                Members = Enumerable.Range(0, 1000)
                    .ToImmutableDictionary(index => index.ToString(CultureInfo.InvariantCulture), _ => 0L),
            },
        };
        SavedItemsAggregate historical = reducer.Reduce(
            full,
            new ReferenceAdded
            {
                SourceId = "recorded",
                ObservedSourceVersion = 8,
            });
        Assert.Equal(1001, historical.References.Members.Count);
        Assert.Equal(8, historical.References.Members["recorded"]);
        Assert.Equal(1000, full.References.Members.Count);
    }
}