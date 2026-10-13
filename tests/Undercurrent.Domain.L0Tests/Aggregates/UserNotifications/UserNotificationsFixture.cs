using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Aggregates;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;
using Mississippi.Undercurrent.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;

/// <summary>Provides deterministic notification requests, history, and aggregate scenarios.</summary>
internal static class UserNotificationsFixture
{
    /// <summary>Gets the fixed server time used by notification history.</summary>
    public static DateTimeOffset CreatedAt { get; } = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Checks commands that intentionally emit no events.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="handler">The command handler.</param>
    /// <param name="command">The command.</param>
    /// <param name="state">The inbox state.</param>
    public static void AssertSuccessfulNoOp<TCommand>(
        CommandHandlerBase<TCommand, UserNotificationsAggregate> handler,
        TCommand command,
        UserNotificationsAggregate? state
    )
    {
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
        Assert.True(result.Success);
        Assert.Empty(result.Value);
    }

    /// <summary>Builds an original request with a fixed deadline.</summary>
    /// <param name="creationKey">The stable creation key.</param>
    /// <param name="createdAt">The optional original request time.</param>
    /// <returns>The original request.</returns>
    public static CreateNotification CreateCommand(
        string creationKey = "notice",
        DateTimeOffset? createdAt = null
    )
    {
        DateTimeOffset time = createdAt ?? CreatedAt;
        return new()
        {
            CreationKey = creationKey,
            ExpiresAt = (DateTimeOffset.MaxValue - time) >= TimeSpan.FromDays(7)
                ? time.AddDays(7)
                : DateTimeOffset.MaxValue,
            Title = "Ready",
            Description = "The deployment is ready.",
            IconKey = "deployment",
            NavigationTarget = "/deployments/42",
        };
    }

    /// <summary>Builds a recorded creation with a known ID for lifecycle replay.</summary>
    /// <param name="notificationId">The known historical ID.</param>
    /// <param name="createdAt">The optional creation time.</param>
    /// <returns>The recorded creation.</returns>
    public static NotificationCreated CreateEvent(
        string notificationId = "notice",
        DateTimeOffset? createdAt = null
    )
    {
        DateTimeOffset time = createdAt ?? CreatedAt;
        CreateNotification request = CreateCommand(notificationId, time);
        return new()
        {
            Notification = new()
            {
                NotificationId = notificationId,
                Title = request.Title,
                Description = request.Description,
                IconKey = request.IconKey,
                NavigationTarget = request.NavigationTarget,
                CreatedAt = time,
            },
            CreationKey = request.CreationKey,
            ExpiresAt = request.ExpiresAt.ToUniversalTime(),
            RequestFingerprint = CreationRequestFingerprint.Create(request),
            ExpiredRequestsThrough = time.ToUniversalTime(),
        };
    }

    /// <summary>Builds a handler with captured host policy.</summary>
    /// <param name="timeProvider">The server clock.</param>
    /// <param name="options">The optional host policy.</param>
    /// <returns>The configured creation handler.</returns>
    public static CreateNotificationHandler CreateHandler(
        TimeProvider timeProvider,
        UserNotificationsOptions? options = null
    ) =>
        new(
            timeProvider,
            new(
                Options.Create(
                    new UndercurrentOptions
                    {
                        Notifications = options ?? new(),
                    })),
            CreateSizeLimit(options));

    /// <summary>Builds a read handler with the real snapshot converter.</summary>
    /// <param name="timeProvider">The server clock.</param>
    /// <param name="options">The optional host policy.</param>
    /// <returns>The configured read handler.</returns>
    public static MarkNotificationAsReadHandler CreateReadHandler(
        TimeProvider timeProvider,
        UserNotificationsOptions? options = null
    ) =>
        new(timeProvider, CreateSizeLimit(options));

    /// <summary>Builds a scenario with all notification handlers and reducers.</summary>
    /// <param name="timeProvider">The server clock.</param>
    /// <returns>The configured aggregate scenario.</returns>
    public static AggregateScenario<UserNotificationsAggregate> CreateScenario(
        TimeProvider timeProvider
    ) =>
        new AggregateTestHarness<UserNotificationsAggregate>().WithHandler(CreateHandler(timeProvider))
            .WithHandler(CreateReadHandler(timeProvider))
            .WithHandler<DeleteNotificationHandler>()
            .WithHandler<DeleteNotificationsBeforeHandler>()
            .WithReducer<NotificationCreatedReducer>()
            .WithReducer<NotificationMarkedAsReadReducer>()
            .WithReducer<NotificationDeletedReducer>()
            .WithReducer<NotificationsDeletedReducer>()
            .CreateScenario();

    /// <summary>Builds a state-size check using the registered JSON snapshot path.</summary>
    /// <param name="options">The optional host policy.</param>
    /// <returns>The configured size check.</returns>
    public static UserNotificationStateSizeLimit CreateSizeLimit(
        UserNotificationsOptions? options = null
    )
    {
        ServiceCollection services = new();
        services.AddJsonSerialization();
        services.AddSnapshotStateConverter<UserNotificationsAggregate>();
        using ServiceProvider provider = services.BuildServiceProvider();
        return new(
            new(
                Options.Create(
                    new UndercurrentOptions
                    {
                        Notifications = options ?? new(),
                    })),
            provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>());
    }
}