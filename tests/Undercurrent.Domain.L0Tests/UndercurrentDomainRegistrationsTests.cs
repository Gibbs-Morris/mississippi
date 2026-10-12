using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Domain;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace MississippiTests.Undercurrent.Domain.L0Tests;

/// <summary>
///     Exposes aggregate registration and persistence serialization tests for xUnit discovery.
/// </summary>
public sealed class UndercurrentDomainRegistrationsTests
{
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
        CreateNotification command = new()
        {
            NotificationId = "Notice",
            Title = "Ready",
        };
        OperationResult<IReadOnlyList<object>> created = handler.Handle(command, null);
        Assert.True(created.Success);
        NotificationCreated createdEvent = Assert.IsType<NotificationCreated>(Assert.Single(created.Value));
        UserNotificationsAggregate state = reducer.Reduce(new(), createdEvent);
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
    ///     A missing service collection fails at the public registration boundary.
    /// </summary>
    [Fact]
    public void NullServicesAreRejected() =>
        Assert.Throws<ArgumentNullException>(() => UndercurrentDomainRegistrations.AddUndercurrentDomain(null!));

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
        IRootCommandHandler<UserNotificationsAggregate> handler = provider.GetRequiredService<IRootCommandHandler<UserNotificationsAggregate>>();
        IRootReducer<UserNotificationsAggregate> reducer = provider.GetRequiredService<IRootReducer<UserNotificationsAggregate>>();
        ISnapshotStateConverter<UserNotificationsAggregate> converter = provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>();
        NotificationCreated created = Aggregates.UserNotifications.UserNotificationsFixture.CreateEvent();
        UserNotificationsAggregate state = reducer.Reduce(new(), created);
        OperationResult<IReadOnlyList<object>> read = handler.Handle(new MarkNotificationAsRead { NotificationId = "notice" }, state);
        Assert.True(read.Success);
        state = reducer.Reduce(state, Assert.Single(read.Value));
        UserNotificationsAggregate restored = converter.FromEnvelope(converter.ToEnvelope(state, reducer.GetReducerHash()));
        Assert.Equal(created.Notification with { ReadAt = readAt }, Assert.Single(restored.Notifications).Value);
        OperationResult<IReadOnlyList<object>> deleted = handler.Handle(new DeleteNotification { NotificationId = "notice" }, restored);
        Assert.True(deleted.Success);
        UserNotificationsAggregate remaining = reducer.Reduce(restored, Assert.Single(deleted.Value));
        Assert.Empty(remaining.Notifications);
        Assert.Single(restored.Notifications);
        IEventTypeRegistry registry = provider.GetRequiredService<IEventTypeRegistry>();
        Assert.Equal(typeof(NotificationMarkedAsRead), registry.ResolveType("MISSISSIPPI.UNDERCURRENT.NOTIFICATIONMARKEDASREAD.V1"));
        Assert.Equal(typeof(NotificationDeleted), registry.ResolveType("MISSISSIPPI.UNDERCURRENT.NOTIFICATIONDELETED.V1"));
    }
}
