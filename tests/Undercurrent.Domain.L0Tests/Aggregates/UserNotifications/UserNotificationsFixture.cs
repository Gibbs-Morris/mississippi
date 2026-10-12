using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Aggregates;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;

/// <summary>
///     Provides deterministic notification history and aggregate scenarios for lifecycle tests.
/// </summary>
internal static class UserNotificationsFixture
{
    /// <summary>
    ///     Gets the fixed creation time for deterministic notification history.
    /// </summary>
    public static DateTimeOffset CreatedAt { get; } = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     Checks successful commands that intentionally emit no events.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="handler">The handler under test.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="state">The current inbox state.</param>
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

    /// <summary>
    ///     Builds a complete creation event for replay.
    /// </summary>
    /// <param name="notificationId">The exact notification ID.</param>
    /// <param name="createdAt">The optional creation time override.</param>
    /// <returns>The historical creation event.</returns>
    public static NotificationCreated CreateEvent(
        string notificationId = "notice",
        DateTimeOffset? createdAt = null
    ) =>
        new()
        {
            Notification = new()
            {
                NotificationId = notificationId,
                Title = "Ready",
                Description = "The deployment is ready.",
                IconKey = "deployment",
                TargetUrl = "/deployments/42",
                CreatedAt = createdAt ?? CreatedAt,
            },
        };

    /// <summary>
    ///     Builds a scenario with all notification handlers and reducers.
    /// </summary>
    /// <param name="timeProvider">The deterministic clock.</param>
    /// <returns>The configured scenario.</returns>
    public static AggregateScenario<UserNotificationsAggregate> CreateScenario(
        TimeProvider timeProvider
    ) =>
        new AggregateTestHarness<UserNotificationsAggregate>().WithHandler(new CreateNotificationHandler(timeProvider))
            .WithHandler(new MarkNotificationAsReadHandler(timeProvider))
            .WithHandler<DeleteNotificationHandler>()
            .WithHandler<DeleteNotificationsBeforeHandler>()
            .WithReducer<NotificationCreatedReducer>()
            .WithReducer<NotificationMarkedAsReadReducer>()
            .WithReducer<NotificationDeletedReducer>()
            .WithReducer<NotificationsDeletedReducer>()
            .CreateScenario();
}