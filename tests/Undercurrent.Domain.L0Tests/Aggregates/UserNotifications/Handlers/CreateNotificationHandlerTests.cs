using System;
using System.Collections.Generic;

using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Aggregates;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications.Handlers;

/// <summary>
///     Exposes creation and retry scenarios for xUnit discovery.
/// </summary>
public sealed class CreateNotificationHandlerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider timeProvider = new(CreatedAt);

    private static CreateNotification CreateCommand() =>
        new()
        {
            NotificationId = "notification-1",
            Title = "Ready",
            Description = "The deployment is ready.",
            IconKey = "deployment",
            TargetUrl = "/deployments/42",
        };

    private static NotificationCreated CreateEvent(
        CreateNotification command
    ) =>
        new()
        {
            Notification = new()
            {
                NotificationId = command.NotificationId,
                Title = command.Title,
                Description = command.Description,
                IconKey = command.IconKey,
                TargetUrl = command.TargetUrl,
                CreatedAt = CreatedAt,
            },
        };

    private AggregateScenario<UserNotificationsAggregate> CreateScenario() =>
        new AggregateTestHarness<UserNotificationsAggregate>().WithHandler(new CreateNotificationHandler(timeProvider))
            .WithReducer<NotificationCreatedReducer>()
            .CreateScenario();

    /// <summary>
    ///     Each content field participates in exact-content conflict detection.
    /// </summary>
    /// <param name="field">The content field to change.</param>
    [Theory]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("icon")]
    [InlineData("url")]
    public void ChangedContentAtExistingIdFailsWithoutChangingInbox(
        string field
    )
    {
        CreateNotification original = CreateCommand();
        NotificationCreated previous = CreateEvent(original);
        CreateNotification changed = field switch
        {
            "title" => original with
            {
                Title = "ready",
            },
            "description" => original with
            {
                Description = null,
            },
            "icon" => original with
            {
                IconKey = string.Empty,
            },
            var _ => original with
            {
                TargetUrl = "/deployments/43",
            },
        };
        CreateScenario()
            .Given(previous)
            .When(changed)
            .ThenFails(AggregateErrorCodes.InvalidState)
            .ThenEmitsEvents()
            .ThenState(state => Assert.Equal(previous.Notification, Assert.Single(state.Notifications).Value));
    }

    /// <summary>
    ///     Creating a distinct case-sensitive ID preserves existing notifications.
    /// </summary>
    [Fact]
    public void CreationOfDifferentIdPreservesExistingNotification()
    {
        CreateNotification original = CreateCommand();
        NotificationCreated previous = CreateEvent(original);
        CreateNotification next = original with
        {
            NotificationId = "NOTIFICATION-1",
        };
        CreateScenario()
            .Given(previous)
            .When(next)
            .ThenSucceeds()
            .ThenEmitsEvents(item => Assert.Equal(CreateEvent(next), Assert.IsType<NotificationCreated>(item)))
            .ThenState(state =>
            {
                Assert.Equal(2, state.Notifications.Count);
                Assert.Equal(previous.Notification, state.Notifications[original.NotificationId]);
                Assert.Equal(CreateEvent(next).Notification, state.Notifications[next.NotificationId]);
            });
    }

    /// <summary>
    ///     Creation retains exact inputs and assigns server UTC time.
    /// </summary>
    /// <param name="metadata">The optional metadata value, including null or empty.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("value")]
    public void CreationPreservesContentAndUsesServerUtcTime(
        string? metadata
    )
    {
        timeProvider.SetUtcNow(CreatedAt.ToOffset(TimeSpan.FromHours(2)));
        CreateNotification command = CreateCommand() with
        {
            NotificationId = " external|notification ",
            Title = " Ready ",
            Description = metadata,
            IconKey = metadata,
            TargetUrl = metadata,
        };
        IReadOnlyList<object> events = new CreateNotificationHandler(timeProvider).ShouldSucceed(null, command);
        NotificationCreated created = Assert.IsType<NotificationCreated>(Assert.Single(events));
        Assert.Equal(CreateEvent(command).Notification, created.Notification);
        Assert.Equal(TimeSpan.Zero, created.Notification.CreatedAt.Offset);
    }

    /// <summary>
    ///     Identical retries leave both the inbox and the original timestamp unchanged.
    /// </summary>
    /// <param name="metadata">The optional content used in the original request and retry.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("value")]
    public void IdenticalRetryEmitsNoEventAndPreservesOriginalTimestamp(
        string? metadata
    )
    {
        CreateNotification command = CreateCommand() with
        {
            Description = metadata,
            IconKey = metadata,
            TargetUrl = metadata,
        };
        NotificationCreated previous = CreateEvent(command);
        timeProvider.Advance(TimeSpan.FromDays(1));
        UserNotificationsAggregate state = CreateScenario().Given(previous).State;
        OperationResult<IReadOnlyList<object>> result =
            new CreateNotificationHandler(timeProvider).Handle(command, state);
        Assert.True(result.Success);
        Assert.Empty(result.Value);
        Assert.Equal(previous.Notification, Assert.Single(state.Notifications).Value);
    }

    /// <summary>
    ///     Invalid notification IDs are rejected before state is inspected.
    /// </summary>
    /// <param name="notificationId">The invalid notification identifier.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void InvalidNotificationIdFailsWithInvalidCommand(
        string? notificationId
    ) =>
        new CreateNotificationHandler(timeProvider).ShouldFailWithMessage(
            null,
            CreateCommand() with
            {
                NotificationId = notificationId!,
            },
            AggregateErrorCodes.InvalidCommand,
            "Notification ID is required.");

    /// <summary>
    ///     Invalid titles are rejected even when the ID already exists.
    /// </summary>
    /// <param name="title">The invalid notification heading.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void InvalidTitleFailsBeforeExistingIdConflict(
        string? title
    )
    {
        CreateNotification original = CreateCommand();
        CreateScenario()
            .Given(CreateEvent(original))
            .When(
                original with
                {
                    Title = title!,
                })
            .ThenFails(AggregateErrorCodes.InvalidCommand, "Title is required.")
            .ThenEmitsEvents()
            .ThenState(state => Assert.Equal(
                CreateEvent(original).Notification,
                Assert.Single(state.Notifications).Value));
    }

    /// <summary>
    ///     A handler requires an explicit time provider.
    /// </summary>
    [Fact]
    public void NullTimeProviderIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new CreateNotificationHandler(null!));
}