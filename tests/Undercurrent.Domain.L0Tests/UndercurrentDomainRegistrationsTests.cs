using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Domain;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;

using MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;


namespace MississippiTests.Undercurrent.Domain.L0Tests;

/// <summary>
///     Exposes aggregate registration and persistence serialization tests for xUnit discovery.
/// </summary>
public sealed class UndercurrentDomainRegistrationsTests
{
    private static T RoundTripEvent<T>(
        IBrookEventConverter converter,
        T eventData
    )
        where T : class =>
        Assert.IsType<T>(
            converter.ToDomainEvent(
                Assert.Single(
                    converter.ToStorageEvents(BrookKey.ForType<UserNotificationsAggregate>("user-42"), [eventData]))));

    /// <summary>
    ///     Registered handlers, reducers, event identity, and state conversion work together without a silo.
    /// </summary>
    [Fact]
    public void AggregateRegistrationSupportsCreationAndSnapshotRoundTrip()
    {
        DateTimeOffset createdAt = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(createdAt));
        Assert.Same(services, services.AddUndercurrentDomain());
        services.AddJsonSerialization();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootCommandHandler<UserNotificationsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<UserNotificationsAggregate>>();
        IRootReducer<UserNotificationsAggregate> reducer =
            provider.GetRequiredService<IRootReducer<UserNotificationsAggregate>>();
        ISnapshotStateConverter<UserNotificationsAggregate> converter =
            provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>();
        IBrookEventConverter eventConverter = provider.GetRequiredService<IBrookEventConverter>();
        CreateNotification command = new()
        {
            NotificationId = "Notice",
            Title = "Ready",
            Description = "The deployment is ready.",
            IconKey = "deployment",
            TargetUrl = "/deployments/42",
        };
        OperationResult<IReadOnlyList<object>> created = handler.Handle(command, null);
        Assert.True(created.Success);
        NotificationCreated createdEvent = Assert.IsType<NotificationCreated>(Assert.Single(created.Value));
        NotificationCreated restoredEvent = RoundTripEvent(eventConverter, createdEvent);
        Assert.Equal(createdEvent, restoredEvent);
        UserNotificationsAggregate state = reducer.Reduce(new(), restoredEvent);
        SnapshotEnvelope envelope = converter.ToEnvelope(state, reducer.GetReducerHash());
        UserNotificationsAggregate restored = converter.FromEnvelope(envelope);
        Assert.Equal(createdAt, restored.Notifications["Notice"].CreatedAt);
        Assert.Equal(createdEvent.Notification, Assert.Single(restored.Notifications).Value);
        Assert.False(restored.Notifications.ContainsKey("notice"));
        OperationResult<IReadOnlyList<object>> retry = handler.Handle(command, restored);
        Assert.True(retry.Success);
        Assert.Empty(retry.Value);
        IEventTypeRegistry registry = provider.GetRequiredService<IEventTypeRegistry>();
        Assert.Equal(
            typeof(NotificationCreated),
            registry.ResolveType("MISSISSIPPI.UNDERCURRENT.NOTIFICATIONCREATED.V1"));
    }

    /// <summary>
    ///     Registered read and delete handlers work after a snapshot round trip.
    /// </summary>
    [Fact]
    public void AggregateRegistrationSupportsReadAndDeletion()
    {
        DateTimeOffset readAt = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(readAt));
        services.AddUndercurrentDomain();
        services.AddJsonSerialization();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootCommandHandler<UserNotificationsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<UserNotificationsAggregate>>();
        IRootReducer<UserNotificationsAggregate> reducer =
            provider.GetRequiredService<IRootReducer<UserNotificationsAggregate>>();
        ISnapshotStateConverter<UserNotificationsAggregate> converter =
            provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>();
        IBrookEventConverter eventConverter = provider.GetRequiredService<IBrookEventConverter>();
        NotificationCreated created = UserNotificationsFixture.CreateEvent();
        UserNotificationsAggregate state = reducer.Reduce(new(), created);
        OperationResult<IReadOnlyList<object>> read = handler.Handle(
            new MarkNotificationAsRead
            {
                NotificationId = "notice",
            },
            state);
        Assert.True(read.Success);
        NotificationMarkedAsRead readEvent = Assert.IsType<NotificationMarkedAsRead>(Assert.Single(read.Value));
        NotificationMarkedAsRead restoredRead = RoundTripEvent(eventConverter, readEvent);
        Assert.Equal(readEvent, restoredRead);
        state = reducer.Reduce(state, restoredRead);
        UserNotificationsAggregate restored =
            converter.FromEnvelope(converter.ToEnvelope(state, reducer.GetReducerHash()));
        Assert.Equal(
            created.Notification with
            {
                ReadAt = readAt,
            },
            Assert.Single(restored.Notifications).Value);
        OperationResult<IReadOnlyList<object>> deleted = handler.Handle(
            new DeleteNotification
            {
                NotificationId = "notice",
            },
            restored);
        Assert.True(deleted.Success);
        NotificationDeleted deletedEvent = Assert.IsType<NotificationDeleted>(Assert.Single(deleted.Value));
        NotificationDeleted restoredDeletion = RoundTripEvent(eventConverter, deletedEvent);
        Assert.Equal(deletedEvent, restoredDeletion);
        UserNotificationsAggregate remaining = reducer.Reduce(restored, restoredDeletion);
        Assert.Empty(remaining.Notifications);
        Assert.Single(restored.Notifications);
        IEventTypeRegistry registry = provider.GetRequiredService<IEventTypeRegistry>();
        Assert.Equal(
            typeof(NotificationMarkedAsRead),
            registry.ResolveType("MISSISSIPPI.UNDERCURRENT.NOTIFICATIONMARKEDASREAD.V1"));
        Assert.Equal(
            typeof(NotificationDeleted),
            registry.ResolveType("MISSISSIPPI.UNDERCURRENT.NOTIFICATIONDELETED.V1"));
    }

    /// <summary>
    ///     Registered cleanup records serializable IDs and applies them through the root reducer.
    /// </summary>
    [Fact]
    public void AggregateRegistrationSupportsSerializedCleanup()
    {
        DateTimeOffset now = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddUndercurrentDomain();
        services.AddJsonSerialization();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootCommandHandler<UserNotificationsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<UserNotificationsAggregate>>();
        IRootReducer<UserNotificationsAggregate> reducer =
            provider.GetRequiredService<IRootReducer<UserNotificationsAggregate>>();
        ISerializationProvider serializer = provider.GetRequiredService<ISerializationProvider>();
        IBrookEventConverter eventConverter = provider.GetRequiredService<IBrookEventConverter>();
        DeleteNotificationsBefore command = new()
        {
            Before = now.AddDays(-7).AddTicks(1234).ToOffset(TimeSpan.FromHours(2)),
        };
        UserNotificationsAggregate state = reducer.Reduce(
            new(),
            UserNotificationsFixture.CreateEvent("old", command.Before.AddTicks(-1)));
        state = reducer.Reduce(state, UserNotificationsFixture.CreateEvent("boundary", command.Before));
        DeleteNotificationsBefore restoredCommand =
            serializer.Deserialize<DeleteNotificationsBefore>(serializer.Serialize(command));
        Assert.Equal(command.Before, restoredCommand.Before);
        Assert.Equal(command.Before.Offset, restoredCommand.Before.Offset);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(restoredCommand, state);
        Assert.True(result.Success);
        NotificationsDeleted deleted = Assert.IsType<NotificationsDeleted>(Assert.Single(result.Value));
        NotificationsDeleted restored = RoundTripEvent(eventConverter, deleted);
        Assert.Equal(deleted.NotificationIds, restored.NotificationIds);
        Assert.Equal(deleted.OlderThan, restored.OlderThan);
        Assert.Equal(command.Before.ToUniversalTime(), restored.OlderThan);
        Assert.Equal(TimeSpan.Zero, restored.OlderThan.Offset);
        UserNotificationsAggregate remaining = reducer.Reduce(state, restored);
        Assert.Equal("boundary", Assert.Single(remaining.Notifications).Key);
        Assert.Equal(2, state.Notifications.Count);
        Assert.Equal(
            typeof(NotificationsDeleted),
            provider.GetRequiredService<IEventTypeRegistry>()
                .ResolveType("MISSISSIPPI.UNDERCURRENT.NOTIFICATIONSDELETED.V1"));
    }

    /// <summary>
    ///     A missing service collection fails at the public registration boundary.
    /// </summary>
    [Fact]
    public void NullServicesAreRejected() =>
        Assert.Throws<ArgumentNullException>(() => UndercurrentDomainRegistrations.AddUndercurrentDomain(null!));
}