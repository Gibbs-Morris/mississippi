using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationCounts;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationList;
using Mississippi.Undercurrent.Domain;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;

using MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;

using Moq;

using Orleans;
using Orleans.Serialization;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Projections.UserNotificationList;

/// <summary>Compares the notification list and its badge counters with the aggregate's recorded transitions.</summary>
public sealed class NotificationProjectionTests
{
    /// <summary>Checks that both read models match the complete retained aggregate inbox.</summary>
    /// <param name="aggregate">The authoritative retained state.</param>
    /// <param name="counts">The badge counters.</param>
    /// <param name="list">The ordered presentation state.</param>
    private static void AssertMatches(
        UserNotificationsAggregate aggregate,
        UserNotificationCountsProjection counts,
        UserNotificationListProjection list
    )
    {
        Assert.Equal(aggregate.Notifications.Count, counts.TotalCount);
        Assert.Equal(
            aggregate.Notifications.Values.Count(notification => !notification.ReadAt.HasValue),
            counts.UnreadCount);
        Assert.Equal(
            aggregate.NotificationOrder.Reverse().Select(id => aggregate.Notifications[id]).ToArray(),
            list.Notifications.ToArray());
    }

    /// <summary>Executes a real command and applies every emitted fact to the aggregate and both views.</summary>
    /// <param name="provider">The registered domain services.</param>
    /// <param name="command">The requested mutation.</param>
    /// <param name="aggregate">The authoritative state to advance.</param>
    /// <param name="counts">The count state to advance.</param>
    /// <param name="list">The list state to advance.</param>
    /// <returns>The emitted facts, including an empty set for retries.</returns>
    private static IReadOnlyList<object> Execute(
        ServiceProvider provider,
        object command,
        ref UserNotificationsAggregate aggregate,
        ref UserNotificationCountsProjection counts,
        ref UserNotificationListProjection list
    )
    {
        OperationResult<IReadOnlyList<object>> result = provider
            .GetRequiredService<IRootCommandHandler<UserNotificationsAggregate>>()
            .Handle(command, aggregate);
        Assert.True(result.Success, result.ErrorMessage);
        foreach (object recorded in result.Value)
        {
            Reduce(provider, recorded, ref aggregate, ref counts, ref list);
        }

        AssertMatches(aggregate, counts, list);
        return result.Value;
    }

    /// <summary>Builds the actual domain registrations and persistence/transport converters without a silo.</summary>
    /// <param name="clock">The controlled server clock.</param>
    /// <returns>The disposable service provider.</returns>
    private static ServiceProvider Provider(
        FakeTimeProvider clock
    )
    {
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(clock);
        services.AddJsonSerialization();
        services.AddSerializer(builder => builder.AddAssembly(typeof(UserNotificationCountsProjection).Assembly));
        services.AddUndercurrentDomain(options =>
        {
            options.Notifications.MaxNotifications = 2;
            options.Notifications.MaxCreationReceipts = 8;
        });
        return services.BuildServiceProvider();
    }

    /// <summary>Replays one fact through all three registered reducers and verifies consistent immutable results.</summary>
    /// <param name="provider">The registered domain services.</param>
    /// <param name="recorded">The recorded fact.</param>
    /// <param name="aggregate">The authoritative state to advance.</param>
    /// <param name="counts">The count state to advance.</param>
    /// <param name="list">The list state to advance.</param>
    private static void Reduce(
        ServiceProvider provider,
        object recorded,
        ref UserNotificationsAggregate aggregate,
        ref UserNotificationCountsProjection counts,
        ref UserNotificationListProjection list
    )
    {
        UserNotificationsAggregate nextAggregate = provider
            .GetRequiredService<IRootReducer<UserNotificationsAggregate>>()
            .Reduce(aggregate, recorded);
        UserNotificationCountsProjection nextCounts = provider
            .GetRequiredService<IRootReducer<UserNotificationCountsProjection>>()
            .Reduce(counts, recorded);
        UserNotificationListProjection nextList = provider
            .GetRequiredService<IRootReducer<UserNotificationListProjection>>()
            .Reduce(list, recorded);
        Assert.NotSame(aggregate, nextAggregate);
        Assert.NotSame(counts, nextCounts);
        Assert.NotSame(list, nextList);
        aggregate = nextAggregate;
        counts = nextCounts;
        list = nextList;
        AssertMatches(aggregate, counts, list);
    }

    /// <summary>Undercurrent alone provides the documented generic reader factory when host dependencies are available.</summary>
    [Fact]
    public void DomainRegistrationResolvesGenericProjectionFactory()
    {
        ServiceCollection services = new();
        services.AddLogging();
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        IUxProjectionGrain<UserNotificationCountsProjection> counts =
            Mock.Of<IUxProjectionGrain<UserNotificationCountsProjection>>();
        IUxProjectionGrain<UserNotificationListProjection> list =
            Mock.Of<IUxProjectionGrain<UserNotificationListProjection>>();
        grains.Setup(factory => factory.GetGrain<IUxProjectionGrain<UserNotificationCountsProjection>>("user", null))
            .Returns(counts);
        grains.Setup(factory => factory.GetGrain<IUxProjectionGrain<UserNotificationListProjection>>("user", null))
            .Returns(list);
        services.AddSingleton(grains.Object);
        services.AddUndercurrentDomain();
        using ServiceProvider provider = services.BuildServiceProvider();
        IUxProjectionGrainFactory factory = provider.GetRequiredService<IUxProjectionGrainFactory>();
        Assert.Same(counts, factory.GetUxProjectionGrain<UserNotificationCountsProjection>("user"));
        Assert.Same(list, factory.GetUxProjectionGrain<UserNotificationListProjection>("user"));
    }

    /// <summary>Empty and absent-ID facts cannot create badge entries or negative counters.</summary>
    [Fact]
    public void EmptyAndAbsentIdFactsPreserveEmptyViews()
    {
        using ServiceProvider provider = Provider(new(UserNotificationsFixture.CreatedAt));
        object[] facts =
        [
            new NotificationMarkedAsRead
            {
                NotificationId = "missing",
                ReadAt = UserNotificationsFixture.CreatedAt,
            },
            new NotificationDeleted
            {
                NotificationId = "missing",
            },
            new NotificationsDeleted
            {
                NotificationIds = ["missing"],
                OlderThan = DateTimeOffset.MaxValue,
            },
        ];
        foreach (object fact in facts)
        {
            UserNotificationCountsProjection counts = provider
                .GetRequiredService<IRootReducer<UserNotificationCountsProjection>>()
                .Reduce(new(), fact);
            UserNotificationListProjection list = provider
                .GetRequiredService<IRootReducer<UserNotificationListProjection>>()
                .Reduce(new(), fact);
            Assert.Equal(0, counts.TotalCount);
            Assert.Equal(0, counts.UnreadCount);
            Assert.Empty(list.Notifications);
        }
    }

    /// <summary>Both views remain consistent through equal times, rollback, read/unread eviction and cleanup.</summary>
    [Fact]
    public void LifecycleMatchesTheRetainedAggregateInbox()
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt);
        using ServiceProvider provider = Provider(clock);
        UserNotificationsAggregate aggregate = new();
        UserNotificationCountsProjection counts = new();
        UserNotificationListProjection list = new();
        AssertMatches(aggregate, counts, list);
        Execute(provider, UserNotificationsFixture.CreateCommand("first"), ref aggregate, ref counts, ref list);
        string first = Assert.Single(list.Notifications).NotificationId;
        Execute(provider, UserNotificationsFixture.CreateCommand("second"), ref aggregate, ref counts, ref list);
        string second = list.Notifications[0].NotificationId;
        Assert.Equal([second, first], list.Notifications.Select(notification => notification.NotificationId).ToArray());
        UserNotificationListProjection beforeRead = list;
        UserNotificationCountsProjection beforeCounts = counts;
        clock.Advance(TimeSpan.FromMinutes(1));
        MarkNotificationAsRead read = new()
        {
            NotificationId = first,
        };
        Execute(provider, read, ref aggregate, ref counts, ref list);
        Assert.Equal(1, counts.UnreadCount);
        Assert.Equal(2, beforeCounts.UnreadCount);
        Assert.All(beforeRead.Notifications, notification => Assert.Null(notification.ReadAt));
        Assert.Equal(UserNotificationsFixture.CreatedAt.AddMinutes(1), list.Notifications[1].ReadAt);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Empty(Execute(provider, read, ref aggregate, ref counts, ref list));
        Assert.Equal(UserNotificationsFixture.CreatedAt.AddMinutes(1), list.Notifications[1].ReadAt);
        FakeTimeProvider earlierClock = new(UserNotificationsFixture.CreatedAt.AddMinutes(-1));
        using ServiceProvider earlierProvider = Provider(earlierClock);
        Execute(
            earlierProvider,
            UserNotificationsFixture.CreateCommand("third", earlierClock.GetUtcNow()),
            ref aggregate,
            ref counts,
            ref list);
        string third = list.Notifications[0].NotificationId;
        Assert.Equal([third, second], list.Notifications.Select(notification => notification.NotificationId).ToArray());
        Assert.True(list.Notifications[0].CreatedAt < list.Notifications[1].CreatedAt);
        Assert.Equal(2, counts.UnreadCount);
        Execute(
            earlierProvider,
            UserNotificationsFixture.CreateCommand("fourth", earlierClock.GetUtcNow()),
            ref aggregate,
            ref counts,
            ref list);
        string fourth = list.Notifications[0].NotificationId;
        Assert.Equal([fourth, third], list.Notifications.Select(notification => notification.NotificationId).ToArray());
        Execute(
            provider,
            new MarkNotificationAsRead
            {
                NotificationId = fourth,
            },
            ref aggregate,
            ref counts,
            ref list);
        Execute(
            provider,
            new DeleteNotification
            {
                NotificationId = fourth,
            },
            ref aggregate,
            ref counts,
            ref list);
        Assert.Equal(1, counts.UnreadCount);
        clock.Advance(TimeSpan.FromMinutes(2));
        Execute(
            provider,
            UserNotificationsFixture.CreateCommand("fifth", clock.GetUtcNow()),
            ref aggregate,
            ref counts,
            ref list);
        string fifth = list.Notifications[0].NotificationId;
        Execute(
            provider,
            new DeleteNotificationsBefore
            {
                Before = clock.GetUtcNow(),
            },
            ref aggregate,
            ref counts,
            ref list);
        Assert.Equal(fifth, Assert.Single(list.Notifications).NotificationId);
        Assert.Equal(1, counts.UnreadCount);
        Execute(
            provider,
            new DeleteNotification
            {
                NotificationId = fifth,
            },
            ref aggregate,
            ref counts,
            ref list);
        Assert.Empty(list.Notifications);
        Assert.Equal(0, counts.TotalCount);
        Assert.Equal(0, counts.UnreadCount);
        UserNotificationsAggregate otherAggregate = new();
        UserNotificationCountsProjection otherCounts = new();
        UserNotificationListProjection otherList = new();
        Execute(provider, UserNotificationsFixture.CreateCommand(), ref otherAggregate, ref otherCounts, ref otherList);
        Assert.Single(otherList.Notifications);
        Assert.Empty(list.Notifications);
        Assert.Equal(0, counts.TotalCount);
        Assert.Equal(2, beforeCounts.TotalCount);
    }

    /// <summary>Public read models share the aggregate stream while keeping separate stable snapshot identities.</summary>
    [Fact]
    public void ReadModelIdentitiesAndPublicSurfaceAreDistinct()
    {
        Type counts = typeof(UserNotificationCountsProjection);
        Type list = typeof(UserNotificationListProjection);
        Assert.True(counts.IsPublic);
        Assert.True(list.IsPublic);
        Assert.Equal(
            ["TotalCount", "UnreadCount"],
            counts.GetProperties().Select(property => property.Name).Order().ToArray());
        Assert.Equal(
            typeof(UserNotificationsAggregate).GetCustomAttribute<BrookNameAttribute>()?.BrookName,
            counts.GetCustomAttribute<BrookNameAttribute>()?.BrookName);
        Assert.Equal(
            counts.GetCustomAttribute<BrookNameAttribute>()?.BrookName,
            list.GetCustomAttribute<BrookNameAttribute>()?.BrookName);
        Assert.Equal(
            "MISSISSIPPI.UNDERCURRENT.USERNOTIFICATIONCOUNTS.V1",
            counts.GetCustomAttribute<SnapshotStorageNameAttribute>()?.StorageName);
        Assert.Equal(
            "MISSISSIPPI.UNDERCURRENT.USERNOTIFICATIONLIST.V1",
            list.GetCustomAttribute<SnapshotStorageNameAttribute>()?.StorageName);
        Assert.NotEqual(
            counts.GetCustomAttribute<SnapshotStorageNameAttribute>()?.StorageName,
            list.GetCustomAttribute<SnapshotStorageNameAttribute>()?.StorageName);
        using ServiceProvider provider = Provider(new(UserNotificationsFixture.CreatedAt));
        ISnapshotTypeRegistry registry = provider.GetRequiredService<ISnapshotTypeRegistry>();
        Assert.Equal(counts, registry.ResolveType("MISSISSIPPI.UNDERCURRENT.USERNOTIFICATIONCOUNTS.V1"));
        Assert.Equal(list, registry.ResolveType("MISSISSIPPI.UNDERCURRENT.USERNOTIFICATIONLIST.V1"));
    }

    /// <summary>Internal count bookkeeping and list content survive persistence and Orleans transport before more facts.</summary>
    /// <param name="transport">Whether to use Orleans transport instead of snapshot conversion.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestoredProjectionsContinueTrackingReadAndDeletedIds(
        bool transport
    )
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt);
        using ServiceProvider provider = Provider(clock);
        UserNotificationsAggregate aggregate = new();
        UserNotificationCountsProjection counts = new();
        UserNotificationListProjection list = new();
        Reduce(provider, UserNotificationsFixture.CreateEvent(), ref aggregate, ref counts, ref list);
        Reduce(provider, UserNotificationsFixture.CreateEvent("Notice"), ref aggregate, ref counts, ref list);
        Execute(
            provider,
            new MarkNotificationAsRead
            {
                NotificationId = "notice",
            },
            ref aggregate,
            ref counts,
            ref list);
        UserNotificationListProjection original = list;
        if (transport)
        {
            Serializer serializer = provider.GetRequiredService<Serializer>();
            counts = Assert.IsType<UserNotificationCountsProjection>(
                serializer.Deserialize<UserNotificationCountsProjection>(serializer.SerializeToArray(counts)));
            list = Assert.IsType<UserNotificationListProjection>(
                serializer.Deserialize<UserNotificationListProjection>(serializer.SerializeToArray(list)));
        }
        else
        {
            ISnapshotStateConverter<UserNotificationCountsProjection> countSnapshots =
                provider.GetRequiredService<ISnapshotStateConverter<UserNotificationCountsProjection>>();
            ISnapshotStateConverter<UserNotificationListProjection> listSnapshots =
                provider.GetRequiredService<ISnapshotStateConverter<UserNotificationListProjection>>();
            counts = countSnapshots.FromEnvelope(
                countSnapshots.ToEnvelope(
                    counts,
                    provider.GetRequiredService<IRootReducer<UserNotificationCountsProjection>>().GetReducerHash()));
            list = listSnapshots.FromEnvelope(
                listSnapshots.ToEnvelope(
                    list,
                    provider.GetRequiredService<IRootReducer<UserNotificationListProjection>>().GetReducerHash()));
        }

        AssertMatches(aggregate, counts, list);
        Assert.Equal(original.Notifications.ToArray(), list.Notifications.ToArray());
        Execute(
            provider,
            new DeleteNotification
            {
                NotificationId = "notice",
            },
            ref aggregate,
            ref counts,
            ref list);
        Assert.Equal("Notice", Assert.Single(list.Notifications).NotificationId);
        Assert.Equal(1, counts.TotalCount);
        Assert.Equal(1, counts.UnreadCount);
        Execute(
            provider,
            new MarkNotificationAsRead
            {
                NotificationId = "Notice",
            },
            ref aggregate,
            ref counts,
            ref list);
        Assert.Equal(0, counts.UnreadCount);
        Assert.Single(original.Notifications.Where(notification => notification.ReadAt.HasValue));
    }
}