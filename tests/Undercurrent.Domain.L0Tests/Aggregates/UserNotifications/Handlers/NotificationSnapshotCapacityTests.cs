using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications.Handlers;

/// <summary>Verifies byte capacity through actual command admission, reducers and snapshot serialization.</summary>
public sealed class NotificationSnapshotCapacityTests
{
    private static UserNotificationsAggregate ApplyCreation(
        FakeTimeProvider clock,
        CreateNotification request,
        UserNotificationsAggregate? state = null,
        UserNotificationsOptions? options = null
    )
    {
        OperationResult<IReadOnlyList<object>> result = UserNotificationsFixture.CreateHandler(clock, options)
            .Handle(request, state);
        Assert.True(result.Success, result.ErrorMessage);
        return new NotificationCreatedReducer().Reduce(
            state ?? new(),
            Assert.IsType<NotificationCreated>(Assert.Single(result.Value)));
    }

    private static ServiceProvider Provider(
        FakeTimeProvider clock,
        UserNotificationsOptions? options = null
    )
    {
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(clock);
        services.AddJsonSerialization();
        services.AddUndercurrentDomain(configured => configured.Notifications = options ?? new());
        return services.BuildServiceProvider();
    }

    private static int Size(
        UserNotificationsAggregate state
    )
    {
        using ServiceProvider provider = Provider(new(UserNotificationsFixture.CreatedAt));
        return provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>()
            .ToEnvelope(state, string.Empty)
            .Data.Length;
    }

    /// <summary>The exact serialized creation boundary succeeds and the first excess byte fails without events.</summary>
    /// <param name="delta">The configured capacity relative to the candidate size.</param>
    /// <param name="success">Whether the command should succeed.</param>
    [Theory]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    public void CreationChecksExactPayloadBoundary(
        int delta,
        bool success
    )
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt);
        CreateNotification request = UserNotificationsFixture.CreateCommand();
        int size = Size(ApplyCreation(clock, request));
        using ServiceProvider provider = Provider(
            clock,
            new()
            {
                MaxSnapshotSizeBytes = size + delta,
            });
        OperationResult<IReadOnlyList<object>> result = provider
            .GetRequiredService<IRootCommandHandler<UserNotificationsAggregate>>()
            .Handle(request, null);
        Assert.Equal(success, result.Success);
        if (success)
        {
            Assert.True(result.Success);
            UserNotificationsAggregate state = provider.GetRequiredService<IRootReducer<UserNotificationsAggregate>>()
                .Reduce(new(), Assert.Single(result.Value));
            Assert.Equal(size, Size(state));
        }
        else
        {
            Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
            Assert.Equal("Notification snapshot capacity is exhausted.", result.ErrorMessage);
            Assert.Null(result.Value);
        }
    }

    /// <summary>Escape-heavy input cannot fill the count limits with an oversized snapshot.</summary>
    [Fact]
    public void DefaultCapacityBoundsEscapedStateBeforeCountCaps()
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt);
        using ServiceProvider provider = Provider(clock);
        IRootCommandHandler<UserNotificationsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<UserNotificationsAggregate>>();
        IRootReducer<UserNotificationsAggregate> reducer =
            provider.GetRequiredService<IRootReducer<UserNotificationsAggregate>>();
        UserNotificationsAggregate state = new();
        OperationResult<IReadOnlyList<object>> result = default!;
        for (int index = 0; index < 100; index++)
        {
            CreateNotification request = new()
            {
                CreationKey = index.ToString(CultureInfo.InvariantCulture).PadLeft(256, '\u0001'),
                Title = new('\u0001', 200),
                Description = new('\u0001', 2_000),
                IconKey = new('\u0001', 128),
                NavigationTarget = new('\u0001', 2_048),
                ExpiresAt = UserNotificationsFixture.CreatedAt.AddDays(7),
            };
            result = handler.Handle(request, state);
            if (!result.Success)
            {
                break;
            }

            state = reducer.Reduce(state, Assert.Single(result.Value));
        }

        Assert.False(result.Success);
        Assert.Equal("Notification snapshot capacity is exhausted.", result.ErrorMessage);
        Assert.Null(result.Value);
        Assert.InRange(state.Notifications.Count, 1, 999);
        Assert.Equal(state.Notifications.Count, state.CreationReceipts.Count);
        SnapshotEnvelope envelope = provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>()
            .ToEnvelope(state, reducer.GetReducerHash());
        Assert.InRange(envelope.Data.Length, 1, 1_000_000);

        // The Cosmos provider stores byte[] as base64; retain generous room for its metadata.
        byte[] document = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                data = envelope.Data.AsSpan().ToArray(),
                metadata = new string('m', 4_096),
            });
        Assert.True(document.Length < 2_000_000);
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"AcceptedNotifications={state.Notifications.Count}; PayloadBytes={envelope.Data.Length}; RepresentativeDocumentBytes={document.Length}");
    }

    /// <summary>A rejected rolling creation cannot evict the old unread notification or forget its receipt.</summary>
    [Fact]
    public void FailedAdmissionPreservesInboxAndRetryHistory()
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt);
        CreateNotification original = UserNotificationsFixture.CreateCommand("first");
        UserNotificationsAggregate state = ApplyCreation(clock, original);
        UserNotificationsOptions policy = new()
        {
            MaxNotifications = 1,
            MaxCreationReceipts = 3,
            MaxSnapshotSizeBytes = Size(state),
        };
        OperationResult<IReadOnlyList<object>> result = UserNotificationsFixture.CreateHandler(clock, policy)
            .Handle(UserNotificationsFixture.CreateCommand("other"), state);
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal("Notification snapshot capacity is exhausted.", result.ErrorMessage);
        Assert.Single(state.Notifications);
        Assert.Null(Assert.Single(state.Notifications).Value.ReadAt);
        Assert.Equal(Assert.Single(state.Notifications).Key, Assert.Single(state.NotificationOrder));
        Assert.Single(state.CreationReceipts);
        Assert.True(state.CreationReceipts.ContainsKey("first"));
        UserNotificationsFixture.AssertSuccessfulNoOp(
            UserNotificationsFixture.CreateHandler(clock, policy),
            original,
            state);
    }

    /// <summary>First-read growth uses the exact boundary, and rejected reads preserve content and unread state.</summary>
    /// <param name="delta">The configured capacity relative to the read state size.</param>
    /// <param name="success">Whether the first read should succeed.</param>
    [Theory]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    public void FirstReadChecksExactPayloadBoundary(
        int delta,
        bool success
    )
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt.AddTicks(1));
        UserNotificationsAggregate state = ApplyCreation(clock, UserNotificationsFixture.CreateCommand());
        string id = Assert.Single(state.Notifications).Key;
        NotificationMarkedAsRead read = new()
        {
            NotificationId = id,
            ReadAt = clock.GetUtcNow(),
        };
        UserNotificationsAggregate expected = new NotificationMarkedAsReadReducer().Reduce(state, read);
        int size = Size(expected);
        Assert.True(size > Size(state));
        OperationResult<IReadOnlyList<object>> result = UserNotificationsFixture.CreateReadHandler(
                clock,
                new()
                {
                    MaxSnapshotSizeBytes = size + delta,
                })
            .Handle(
                new()
                {
                    NotificationId = id,
                },
                state);
        Assert.Equal(success, result.Success);
        Assert.Null(state.Notifications[id].ReadAt);
        if (success)
        {
            Assert.True(result.Success);
            Assert.Equal(read, Assert.IsType<NotificationMarkedAsRead>(Assert.Single(result.Value)));
        }
        else
        {
            Assert.Null(result.Value);
            Assert.Equal("Notification snapshot capacity is exhausted.", result.ErrorMessage);
        }
    }

    /// <summary>Recorded pruning and eviction are sized together so reclaimed capacity can admit a creation.</summary>
    [Fact]
    public void ReclaimedCapacityIsMeasuredAfterTheWholeTransition()
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt);
        UserNotificationsOptions countPolicy = new()
        {
            MaxNotifications = 1,
            MaxCreationReceipts = 4,
        };
        CreateNotification first = UserNotificationsFixture.CreateCommand("first") with
        {
            ExpiresAt = clock.GetUtcNow().AddMinutes(1),
        };
        CreateNotification second = first with
        {
            CreationKey = "other",
        };
        UserNotificationsAggregate state = ApplyCreation(clock, second, ApplyCreation(clock, first), countPolicy);
        clock.Advance(TimeSpan.FromMinutes(1));
        CreateNotification third = UserNotificationsFixture.CreateCommand("third", clock.GetUtcNow());
        UserNotificationsAggregate expected = ApplyCreation(clock, third, state, countPolicy);
        int capacity = Size(expected);
        Assert.True(Size(state) > capacity);
        UserNotificationsAggregate actual = ApplyCreation(
            clock,
            third,
            state,
            new()
            {
                MaxNotifications = 1,
                MaxCreationReceipts = 4,
                MaxSnapshotSizeBytes = capacity,
            });
        Assert.Single(actual.Notifications);
        Assert.Single(actual.CreationReceipts);
        Assert.True(actual.CreationReceipts.ContainsKey("third"));
        Assert.Equal(capacity, Size(actual));
        Assert.Equal(2, state.CreationReceipts.Count);
    }

    /// <summary>Smaller host limits preserve replay, retained retries and deletion from restored oversized state.</summary>
    [Fact]
    public void RestoredHistorySurvivesTightenedPolicy()
    {
        FakeTimeProvider clock = new(UserNotificationsFixture.CreatedAt);
        CreateNotification request = UserNotificationsFixture.CreateCommand();
        UserNotificationsAggregate state = ApplyCreation(clock, request);
        string id = Assert.Single(state.Notifications).Key;
        state = new NotificationMarkedAsReadReducer().Reduce(
            state,
            new()
            {
                NotificationId = id,
                ReadAt = clock.GetUtcNow(),
            });
        using ServiceProvider provider = Provider(
            clock,
            new()
            {
                MaxSnapshotSizeBytes = 1,
            });
        ISnapshotStateConverter<UserNotificationsAggregate> snapshots =
            provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>();
        UserNotificationsAggregate restored = snapshots.FromEnvelope(snapshots.ToEnvelope(state, string.Empty));
        IRootCommandHandler<UserNotificationsAggregate> handler =
            provider.GetRequiredService<IRootCommandHandler<UserNotificationsAggregate>>();
        IRootReducer<UserNotificationsAggregate> reducer =
            provider.GetRequiredService<IRootReducer<UserNotificationsAggregate>>();
        Assert.Single(restored.Notifications);
        OperationResult<IReadOnlyList<object>> retry = handler.Handle(request, restored);
        Assert.True(retry.Success);
        Assert.Empty(retry.Value);
        OperationResult<IReadOnlyList<object>> repeatedRead = handler.Handle(
            new MarkNotificationAsRead
            {
                NotificationId = id,
            },
            restored);
        Assert.True(repeatedRead.Success);
        Assert.Empty(repeatedRead.Value);
        OperationResult<IReadOnlyList<object>> creation = handler.Handle(
            UserNotificationsFixture.CreateCommand("next"),
            restored);
        Assert.False(creation.Success);
        Assert.Null(creation.Value);
        OperationResult<IReadOnlyList<object>> deletion = handler.Handle(
            new DeleteNotification
            {
                NotificationId = id,
            },
            restored);
        Assert.True(deletion.Success);
        UserNotificationsAggregate deleted = reducer.Reduce(restored, Assert.Single(deletion.Value));
        Assert.Empty(deleted.Notifications);
        Assert.Single(deleted.CreationReceipts);
        Assert.Equal("Notification has already been deleted.", handler.Handle(request, deleted).ErrorMessage);
        OperationResult<IReadOnlyList<object>> cleanup = handler.Handle(
            new DeleteNotificationsBefore
            {
                Before = DateTimeOffset.MaxValue,
            },
            restored);
        Assert.True(cleanup.Success);
        Assert.Empty(reducer.Reduce(restored, Assert.Single(cleanup.Value)).Notifications);
    }

    /// <summary>State size checks require their actual policy and converter dependencies.</summary>
    [Fact]
    public void SizeLimitDependenciesAreRequired()
    {
        UserNotificationPolicy policy = new(Options.Create(new UndercurrentOptions()));
        using ServiceProvider provider = Provider(new(UserNotificationsFixture.CreatedAt));
        ISnapshotStateConverter<UserNotificationsAggregate> converter =
            provider.GetRequiredService<ISnapshotStateConverter<UserNotificationsAggregate>>();
        Assert.Throws<ArgumentNullException>(() => new UserNotificationStateSizeLimit(null!, converter));
        Assert.Throws<ArgumentNullException>(() => new UserNotificationStateSizeLimit(policy, null!));
    }
}