using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Undercurrent.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications.Handlers;

/// <summary>Verifies generated identities, exact request retries, input bounds, and expiry.</summary>
public sealed class CreateNotificationHandlerTests
{
    private readonly FakeTimeProvider timeProvider = new(UserNotificationsFixture.CreatedAt);

    private static NotificationCreated Create(
        CreateNotificationHandler handler,
        CreateNotification command,
        UserNotificationsAggregate? state = null
    )
    {
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
        Assert.True(result.Success, result.ErrorMessage);
        return Assert.IsType<NotificationCreated>(Assert.Single(result.Value));
    }

    private CreateNotificationHandler CreateHandler(
        UserNotificationsOptions? options = null
    ) =>
        UserNotificationsFixture.CreateHandler(timeProvider, options);

    /// <summary>Every caller-supplied field and the original expiry participate in conflict detection.</summary>
    /// <param name="field">The field to change.</param>
    [Theory]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("icon")]
    [InlineData("url")]
    [InlineData("expiry")]
    public void ChangedRequestAtExistingKeyConflicts(
        string field
    )
    {
        CreateNotification original = UserNotificationsFixture.CreateCommand();
        CreateNotificationHandler handler = CreateHandler();
        NotificationCreated first = Create(handler, original);
        UserNotificationsAggregate state = new NotificationCreatedReducer().Reduce(new(), first);
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
            "url" => original with
            {
                NavigationTarget = "/deployments/43",
            },
            var _ => original with
            {
                ExpiresAt = original.ExpiresAt.AddTicks(1),
            },
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(changed, state);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
        Assert.Equal("Creation key already identifies a different request.", result.ErrorMessage);
        Assert.Null(result.Value);
        Assert.Equal(first.Notification, Assert.Single(state.Notifications).Value);
    }

    /// <summary>Creation preserves exact presentation values while the handler supplies identity and UTC timestamps.</summary>
    /// <param name="metadata">The optional field value.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("value")]
    public void CreationPreservesContentAndGeneratesIdentity(
        string? metadata
    )
    {
        CreateNotification command = UserNotificationsFixture.CreateCommand(" external|creation ") with
        {
            Title = " Ready ",
            Description = metadata,
            IconKey = metadata,
            NavigationTarget = metadata,
            ExpiresAt = UserNotificationsFixture.CreatedAt.AddDays(7).ToOffset(TimeSpan.FromHours(2)),
        };
        NotificationCreated created = Create(CreateHandler(), command);
        Assert.True(Guid.TryParseExact(created.Notification.NotificationId, "D", out Guid _));
        Assert.NotEqual(command.CreationKey, created.Notification.NotificationId);
        Assert.Equal(command.CreationKey, created.CreationKey);
        Assert.Equal(command.Title, created.Notification.Title);
        Assert.Equal(metadata, created.Notification.Description);
        Assert.Equal(metadata, created.Notification.IconKey);
        Assert.Equal(metadata, created.Notification.NavigationTarget);
        Assert.Equal(UserNotificationsFixture.CreatedAt, created.Notification.CreatedAt);
        Assert.Equal(TimeSpan.Zero, created.Notification.CreatedAt.Offset);
        Assert.Equal(command.ExpiresAt.ToUniversalTime(), created.ExpiresAt);
        Assert.Equal(TimeSpan.Zero, created.ExpiresAt.Offset);
    }

    /// <summary>Case-sensitive creation keys create separate generated identities in one inbox.</summary>
    [Fact]
    public void DifferentKeysGenerateDifferentNotificationIds()
    {
        CreateNotificationHandler handler = CreateHandler();
        CreateNotification original = UserNotificationsFixture.CreateCommand("Notice");
        NotificationCreated first = Create(handler, original);
        UserNotificationsAggregate state = new NotificationCreatedReducer().Reduce(new(), first);
        NotificationCreated second = Create(
            handler,
            original with
            {
                CreationKey = "notice",
            },
            state);
        Assert.NotEqual(first.Notification.NotificationId, second.Notification.NotificationId);
        UserNotificationsAggregate result = new NotificationCreatedReducer().Reduce(state, second);
        Assert.Equal(2, result.Notifications.Count);
        Assert.Equal(2, result.CreationReceipts.Count);
        Assert.Equal(first.Notification.NotificationId, result.CreationReceipts["Notice"].NotificationId);
        Assert.Equal(second.Notification.NotificationId, result.CreationReceipts["notice"].NotificationId);
        Assert.Single(state.Notifications);
    }

    /// <summary>Inputs at every default boundary remain accepted.</summary>
    [Fact]
    public void ExactFieldLimitsAreAccepted()
    {
        CreateNotification command = UserNotificationsFixture.CreateCommand(new('k', 256)) with
        {
            Title = new('t', 200),
            Description = new('d', 2_000),
            IconKey = new('i', 128),
            NavigationTarget = new('u', 2_048),
        };
        NotificationCreated created = Create(CreateHandler(), command);
        Assert.Equal(command.Description, created.Notification.Description);
    }

    /// <summary>Request expiration is exclusive and the maximum future deadline is inclusive.</summary>
    /// <param name="ticks">The expiry offset from the server clock.</param>
    /// <param name="success">Whether the deadline is accepted.</param>
    [Theory]
    [InlineData(-1L, false)]
    [InlineData(0L, false)]
    [InlineData(1L, true)]
    [InlineData(6_048_000_000_000L, true)]
    [InlineData(6_048_000_000_001L, false)]
    public void ExpiryBoundariesArePrecise(
        long ticks,
        bool success
    )
    {
        OperationResult<IReadOnlyList<object>> result = CreateHandler()
            .Handle(
                UserNotificationsFixture.CreateCommand() with
                {
                    ExpiresAt = UserNotificationsFixture.CreatedAt.AddTicks(ticks),
                },
                null);
        Assert.Equal(success, result.Success);
        if (!success)
        {
            Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
            Assert.Null(result.Value);
        }
    }

    /// <summary>Matching retries retain the generated identity and initial timestamps.</summary>
    [Fact]
    public void IdenticalRetryEmitsNoEvent()
    {
        CreateNotification command = UserNotificationsFixture.CreateCommand();
        CreateNotificationHandler handler = CreateHandler();
        NotificationCreated created = Create(handler, command);
        UserNotificationsAggregate state = new NotificationCreatedReducer().Reduce(new(), created);
        timeProvider.Advance(TimeSpan.FromDays(1));
        UserNotificationsFixture.AssertSuccessfulNoOp(handler, command, state);
        Assert.Equal(created.Notification, Assert.Single(state.Notifications).Value);
        Assert.Equal(created.ExpiresAt, state.CreationReceipts[command.CreationKey].ExpiresAt);
    }

    /// <summary>Malformed creation keys fail before any state lookup or change.</summary>
    /// <param name="key">The invalid creation key.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void InvalidCreationKeyFails(
        string? key
    )
    {
        OperationResult<IReadOnlyList<object>> result = CreateHandler()
            .Handle(
                UserNotificationsFixture.CreateCommand() with
                {
                    CreationKey = key!,
                },
                null);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
        Assert.Equal("Creation key is required.", result.ErrorMessage);
        Assert.Null(result.Value);
    }

    /// <summary>Malformed titles fail even when a matching key exists.</summary>
    /// <param name="title">The invalid title.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void InvalidTitleFailsBeforeExistingKeyLookup(
        string? title
    )
    {
        CreateNotification command = UserNotificationsFixture.CreateCommand();
        CreateNotificationHandler handler = CreateHandler();
        NotificationCreated created = Create(handler, command);
        UserNotificationsAggregate state = new NotificationCreatedReducer().Reduce(new(), created);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(
            command with
            {
                Title = title!,
            },
            state);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
        Assert.Equal("Title is required.", result.ErrorMessage);
        Assert.Equal(created.Notification, Assert.Single(state.Notifications).Value);
    }

    /// <summary>Malformed UTF-16 cannot be accepted then replaced or rejected by the persistence codec.</summary>
    /// <param name="field">The malformed input field.</param>
    [Theory]
    [InlineData("key")]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("icon")]
    [InlineData("navigation")]
    public void MalformedUnicodeFailsBeforePersistence(
        string field
    )
    {
        string invalid = new((char)0xD800, 1);
        CreateNotification request = UserNotificationsFixture.CreateCommand();
        request = field switch
        {
            "key" => request with
            {
                CreationKey = invalid,
            },
            "title" => request with
            {
                Title = invalid,
            },
            "description" => request with
            {
                Description = invalid,
            },
            "icon" => request with
            {
                IconKey = invalid,
            },
            var _ => request with
            {
                NavigationTarget = invalid,
            },
        };
        OperationResult<IReadOnlyList<object>> result = CreateHandler().Handle(request, null);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
        Assert.Equal("Creation request content must contain valid UTF-16.", result.ErrorMessage);
        Assert.Null(result.Value);
    }

    /// <summary>Server clocks near the maximum supported instant do not overflow lifetime arithmetic.</summary>
    [Fact]
    public void MaximumDateTimeExpiryCanBeAccepted()
    {
        FakeTimeProvider nearMaximum = new(DateTimeOffset.MaxValue.AddTicks(-1));
        NotificationCreated created = Create(
            UserNotificationsFixture.CreateHandler(nearMaximum),
            UserNotificationsFixture.CreateCommand() with
            {
                ExpiresAt = DateTimeOffset.MaxValue,
            });
        Assert.Equal(DateTimeOffset.MaxValue, created.ExpiresAt);
    }

    /// <summary>Required injected dependencies and directly supplied invalid policies are rejected.</summary>
    [Fact]
    public void MissingDependenciesAndInvalidPolicyAreRejected()
    {
        UserNotificationPolicy policy = new(Options.Create(new UndercurrentOptions()));
        Assert.Throws<ArgumentNullException>(() => new CreateNotificationHandler(null!, policy));
        Assert.Throws<ArgumentNullException>(() => new CreateNotificationHandler(timeProvider, null!));
        Assert.Throws<ArgumentNullException>(() => new UserNotificationPolicy(null!));
        Assert.Throws<OptionsValidationException>(() => new UserNotificationPolicy(
            Options.Create(
                new UndercurrentOptions
                {
                    Notifications = new()
                    {
                        MaxNotifications = 0,
                    },
                })));
    }

    /// <summary>Every configured input bound rejects the first excess code unit before eviction.</summary>
    /// <param name="field">The oversized input field.</param>
    [Theory]
    [InlineData("key")]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("icon")]
    [InlineData("url")]
    public void OversizedInputDoesNotEvictExistingNotification(
        string field
    )
    {
        CreateNotificationHandler handler = CreateHandler(
            new()
            {
                MaxNotifications = 1,
                MaxCreationReceipts = 3,
            });
        NotificationCreated first = Create(handler, UserNotificationsFixture.CreateCommand());
        UserNotificationsAggregate state = new NotificationCreatedReducer().Reduce(new(), first);
        CreateNotification command = UserNotificationsFixture.CreateCommand("next");
        command = field switch
        {
            "key" => command with
            {
                CreationKey = new('k', 257),
            },
            "title" => command with
            {
                Title = new('t', 201),
            },
            "description" => command with
            {
                Description = new('d', 2_001),
            },
            "icon" => command with
            {
                IconKey = new('i', 129),
            },
            var _ => command with
            {
                NavigationTarget = new('u', 2_049),
            },
        };
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
        Assert.Null(result.Value);
        Assert.Equal(first.Notification, Assert.Single(state.Notifications).Value);
        Assert.Single(state.CreationReceipts);
        Assert.Equal([first.Notification.NotificationId], state.NotificationOrder);
    }

    /// <summary>Retained request recognition respects the accepted facts after host limits are tightened.</summary>
    [Fact]
    public void RetainedRetryKeepsAcceptedDeadlineAndContentAfterPolicyChange()
    {
        CreateNotification command = UserNotificationsFixture.CreateCommand();
        NotificationCreated created = Create(CreateHandler(), command);
        UserNotificationsAggregate state = new NotificationCreatedReducer().Reduce(new(), created);
        CreateNotificationHandler stricter = CreateHandler(
            new()
            {
                MaxTitleLength = 1,
                MaxCreationKeyLength = 1,
                MaxDescriptionLength = 0,
                MaxIconKeyLength = 0,
                MaxNavigationTargetLength = 0,
                MaxRequestLifetime = TimeSpan.FromHours(1),
            });
        UserNotificationsFixture.AssertSuccessfulNoOp(stricter, command, state);
    }

    /// <summary>Valid surrogate pairs remain supported and preserved exactly.</summary>
    [Fact]
    public void UnicodeSurrogatePairsRemainSupported()
    {
        CreateNotification request = UserNotificationsFixture.CreateCommand() with
        {
            Title = "Ready \U0001F680",
        };
        NotificationCreated created = Create(CreateHandler(), request);
        Assert.Equal(request.Title, created.Notification.Title);
    }

    /// <summary>Unbounded input is rejected before hashing or inspecting retained keys.</summary>
    [Fact]
    public void UnsupportedInputBudgetFails()
    {
        OperationResult<IReadOnlyList<object>> result = CreateHandler()
            .Handle(
                UserNotificationsFixture.CreateCommand() with
                {
                    Description = new('d', 16_385),
                },
                null);
        Assert.False(result.Success);
        Assert.Equal("Creation request content exceeds the supported length budget.", result.ErrorMessage);
    }
}