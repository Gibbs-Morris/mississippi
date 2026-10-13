using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;

/// <summary>Verifies compound event persistence, supported event bounds, and full default state serialization.</summary>
public sealed class NotificationSerializationTests
{
    private static UserNotificationsAggregate BuildState(
        int notifications,
        int receipts,
        bool fullContent
    )
    {
        ImmutableDictionary<string, UserNotification>.Builder inbox =
            ImmutableDictionary.CreateBuilder<string, UserNotification>(StringComparer.Ordinal);
        ImmutableArray<string>.Builder order = ImmutableArray.CreateBuilder<string>();
        ImmutableDictionary<string, NotificationCreationReceipt>.Builder history =
            ImmutableDictionary.CreateBuilder<string, NotificationCreationReceipt>(StringComparer.Ordinal);
        for (int index = 0; index < notifications; index++)
        {
            string id = Id(index);
            inbox.Add(
                id,
                new()
                {
                    NotificationId = id,
                    Title = fullContent ? new('\u0001', 200) : "Ready",
                    Description = fullContent ? new string('\u0001', 2_000) : null,
                    IconKey = fullContent ? new string('\u0001', 128) : null,
                    NavigationTarget = fullContent ? new string('\u0001', 2_048) : null,
                    CreatedAt = UserNotificationsFixture.CreatedAt,
                    ReadAt = UserNotificationsFixture.CreatedAt.AddHours(1),
                });
            order.Add(id);
        }

        for (int index = 0; index < receipts; index++)
        {
            string key = index.ToString(CultureInfo.InvariantCulture).PadLeft(256, '\u0001');
            history.Add(
                key,
                new()
                {
                    NotificationId = Id(index),
                    ExpiresAt = UserNotificationsFixture.CreatedAt.AddDays(7),
                    RequestFingerprint = "V1:" + index.ToString("X64", CultureInfo.InvariantCulture),
                });
        }

        return new()
        {
            Notifications = inbox.ToImmutable(),
            NotificationOrder = order.ToImmutable(),
            CreationReceipts = history.ToImmutable(),
            ExpiredRequestsThrough = UserNotificationsFixture.CreatedAt,
        };
    }

    private static string Id(
        int index
    ) =>
        Guid.ParseExact(index.ToString("x32", CultureInfo.InvariantCulture), "N").ToString("D");

    private static ServiceProvider Provider(
        FakeTimeProvider clock
    )
    {
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(clock);
        services.AddUndercurrentDomain(options =>
        {
            options.Notifications.MaxNotifications = 1;
            options.Notifications.MaxCreationReceipts = 3;
        });
        services.AddJsonSerialization();
        return services.BuildServiceProvider();
    }

    /// <summary>Recorded evictions and receipt cleanup survive event and snapshot conversion together.</summary>
    [Fact]
    public void CompoundCreationAndReceiptPruningRoundTrip()
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt);
        using ServiceProvider provider = Provider(clock);
        IRootCommandHandler<UserNotificationsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<UserNotificationsAggregate>>();
        IRootReducer<UserNotificationsAggregate> reducer =
            provider.GetRequiredService<IRootReducer<UserNotificationsAggregate>>();
        IBrookEventConverter events = provider.GetRequiredService<IBrookEventConverter>();
        ISnapshotStateConverter<UserNotificationsAggregate> snapshots =
            provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>();
        CreateNotification original = UserNotificationsFixture.CreateCommand("first") with
        {
            ExpiresAt = UserNotificationsFixture.CreatedAt.AddMinutes(1),
        };
        UserNotificationsAggregate state = new();
        foreach (CreateNotification command in new[] { original, UserNotificationsFixture.CreateCommand("second") })
        {
            OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
            Assert.True(result.Success);
            state = reducer.Reduce(state, Assert.Single(result.Value));
        }

        clock.Advance(TimeSpan.FromMinutes(1));
        CreateNotification request = UserNotificationsFixture.CreateCommand("third");
        OperationResult<IReadOnlyList<object>> creation = handler.Handle(request, state);
        Assert.True(creation.Success);
        NotificationCreated created = Assert.IsType<NotificationCreated>(Assert.Single(creation.Value));
        BrookEvent stored = Assert.Single(
            events.ToStorageEvents(BrookKey.ForType<UserNotificationsAggregate>("user"), [created]));
        NotificationCreated restoredEvent = Assert.IsType<NotificationCreated>(events.ToDomainEvent(stored));
        Assert.Equal(created.EvictedNotificationIds.ToArray(), restoredEvent.EvictedNotificationIds.ToArray());
        Assert.Equal(created.Notification, restoredEvent.Notification);
        Assert.Equal(created.RequestFingerprint, restoredEvent.RequestFingerprint);
        UserNotificationsAggregate expected = reducer.Reduce(state, created);
        UserNotificationsAggregate restored = snapshots.FromEnvelope(
            snapshots.ToEnvelope(reducer.Reduce(state, restoredEvent), reducer.GetReducerHash()));
        Assert.Equal(expected.NotificationOrder, restored.NotificationOrder);
        Assert.Equal(expected.ExpiredRequestsThrough, restored.ExpiredRequestsThrough);
        Assert.False(restored.CreationReceipts.ContainsKey("first"));
        Assert.Equal(expected.CreationReceipts["second"], restored.CreationReceipts["second"]);
        Assert.Equal(expected.CreationReceipts["third"], restored.CreationReceipts["third"]);
        Assert.True(handler.Handle(request, restored).Success);
        Assert.Equal("Creation request has expired.", handler.Handle(original, restored).ErrorMessage);
    }

    /// <summary>
    ///     Full default state preserves compact receipt history and exposes serialization costs without claiming peak
    ///     memory.
    /// </summary>
    [Fact]
    public void FullDefaultStateRoundTripsAndReportsSizing()
    {
        using ServiceProvider provider = Provider(new(UserNotificationsFixture.CreatedAt));
        ISnapshotStateConverter<UserNotificationsAggregate> snapshots =
            provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>();
        string hash = provider.GetRequiredService<IRootReducer<UserNotificationsAggregate>>().GetReducerHash();
        _ = snapshots.ToEnvelope(new(), hash);
        long beforeBuild = GC.GetAllocatedBytesForCurrentThread();
        UserNotificationsAggregate state = BuildState(1_000, 10_000, true);
        long buildAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeBuild;
        long beforeSerialization = GC.GetAllocatedBytesForCurrentThread();
        SnapshotEnvelope envelope = snapshots.ToEnvelope(state, hash);
        long serializationAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeSerialization;
        UserNotificationsAggregate restored = snapshots.FromEnvelope(envelope);
        Assert.Equal(1_000, restored.Notifications.Count);
        Assert.Equal(10_000, restored.CreationReceipts.Count);
        Assert.Equal(state.NotificationOrder, restored.NotificationOrder);
        Assert.Equal(state.ExpiredRequestsThrough, restored.ExpiredRequestsThrough);
        Assert.Equal(state.Notifications[Id(0)], restored.Notifications[Id(0)]);
        Assert.Equal(
            state.CreationReceipts.First().Value,
            restored.CreationReceipts[state.CreationReceipts.First().Key]);
        Assert.Equal(envelope.Data.Length, envelope.DataSizeBytes);
        TestContext.Current.TestOutputHelper?.WriteLine(
            "DefaultSnapshotDataBytes=" +
            envelope.DataSizeBytes.ToString(CultureInfo.InvariantCulture) +
            "; StateBuildThreadAllocatedBytes=" +
            buildAllocated.ToString(CultureInfo.InvariantCulture) +
            "; SerializationThreadAllocatedBytes=" +
            serializationAllocated.ToString(CultureInfo.InvariantCulture) +
            "; Allocation counters do not measure peak resident memory.");
    }

    /// <summary>Maximum supported eviction count and escaped input budget produce one bounded stored event.</summary>
    [Fact]
    public void MaximumSupportedCreationEventFitsConservativeDataBudget()
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt);
        using ServiceProvider provider = Provider(clock);
        UserNotificationsAggregate state = BuildState(10_000, 0, false);
        CreateNotificationHandler handler = UserNotificationsFixture.CreateHandler(
            clock,
            new()
            {
                MaxNotifications = 1,
                MaxCreationReceipts = 100_000,
                MaxTitleLength = 1,
                MaxCreationKeyLength = 1,
                MaxDescriptionLength = 16_382,
                MaxIconKeyLength = 0,
                MaxNavigationTargetLength = 0,
            });
        CreateNotification request = new()
        {
            CreationKey = "k",
            Title = "t",
            Description = new('\u0001', 16_382),
            ExpiresAt = UserNotificationsFixture.CreatedAt.AddDays(7),
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(request, state);
        Assert.True(result.Success);
        NotificationCreated created = Assert.IsType<NotificationCreated>(Assert.Single(result.Value));
        Assert.Equal(10_000, created.EvictedNotificationIds.Length);
        BrookEvent stored = Assert.Single(
            provider.GetRequiredService<IBrookEventConverter>()
                .ToStorageEvents(BrookKey.ForType<UserNotificationsAggregate>("user"), [created]));
        Assert.InRange(stored.Data.Length, 1, 512_000);
        TestContext.Current.TestOutputHelper?.WriteLine(
            "MaximumSupportedCreationEventDataBytes=" + stored.Data.Length.ToString(CultureInfo.InvariantCulture));
    }
}