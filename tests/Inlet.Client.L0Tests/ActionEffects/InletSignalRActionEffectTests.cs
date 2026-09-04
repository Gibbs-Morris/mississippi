using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Verifies projection subscription, refresh, notification, and reconnection behavior.
/// </summary>
public sealed class InletSignalRActionEffectTests
{
    /// <summary>
    ///     Cancellation while fetching does not report an application failure or a stale successful payload.
    /// </summary>
    /// <param name="refresh">Whether to refresh an existing projection rather than subscribe.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledFetchStopsAfterLoading(
        bool refresh
    )
    {
        await using InletSignalRActionEffectFixture fixture = new();
        fixture.Fetcher
            .Setup(fetcher => fetcher.FetchAsync(typeof(TestProjection), "account-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        IAction request = refresh
            ? new RefreshProjectionAction<TestProjection>("account-1")
            : new SubscribeToProjectionAction<TestProjection>("account-1");
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(Assert.Single(await fixture.RunAsync(request)));
    }

    /// <summary>
    ///     Connection requests establish the connection without fetching a projection.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ConnectionRequestConnectsWithoutFetchingProjection()
    {
        await using InletSignalRActionEffectFixture fixture = new();
        using CancellationTokenSource cancellation = new();
        RequestSignalRConnectionAction request = new();
        Assert.True(fixture.Effect.CanHandle(request));
        Assert.True(fixture.Effect.CanHandle(new SubscribeToProjectionAction<TestProjection>("account-1")));
        Assert.True(fixture.Effect.CanHandle(new UnsubscribeFromProjectionAction<TestProjection>("account-1")));
        Assert.True(fixture.Effect.CanHandle(new RefreshProjectionAction<TestProjection>("account-1")));
        Assert.False(fixture.Effect.CanHandle(new SignalRConnectingAction()));
        Assert.False(fixture.Effect.CanHandle(new ProjectionLoadingAction<TestProjection>("account-1")));
        Assert.Empty(await fixture.RunAsync(request, cancellation.Token));
        fixture.Provider.Verify(provider => provider.EnsureConnectedAsync(cancellation.Token), Times.Once);
        fixture.Fetcher.VerifyNoOtherCalls();
        fixture.Connection.Verify(
            connection => connection.InvokeCoreAsync(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Disposing the effect unregisters notifications and prevents reconnecting prior subscriptions.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task DisposalReleasesCallbackAndClearsSubscriptions()
    {
        await using InletSignalRActionEffectFixture fixture = new();
        await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1"));
        await fixture.Effect.DisposeAsync();
        await fixture.ReconnectAsync();
        fixture.CallbackRegistration.Verify(registration => registration.Dispose(), Times.Once);
        Assert.Empty(fixture.DispatchedActions);
        fixture.Connection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Failed or cancelled subscription requests can be retried without leaving an active local subscription.
    /// </summary>
    /// <param name="cancelled">Whether the operation is cancelled rather than failing.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedSubscriptionCanBeRetried(
        bool cancelled
    )
    {
        await using InletSignalRActionEffectFixture fixture = new();
        Exception failure =
            cancelled ? new OperationCanceledException() : new InvalidOperationException("Hub unavailable.");
        fixture.Connection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        SubscribeToProjectionAction<TestProjection> request = new("account-1");
        List<IAction> failedActions = await fixture.RunAsync(request);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(failedActions[0]);
        Assert.Equal(cancelled ? 1 : 2, failedActions.Count);
        if (!cancelled)
        {
            Assert.Same(failure, Assert.IsType<ProjectionErrorAction<TestProjection>>(failedActions[1]).Error);
        }

        fixture.Fetcher.VerifyNoOtherCalls();
        fixture.Connection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("subscription-2");
        Assert.IsType<ProjectionLoadedAction<TestProjection>>((await fixture.RunAsync(request))[1]);
    }

    /// <summary>
    ///     Fetch errors become error actions and retain the original exception for diagnostics.
    /// </summary>
    /// <param name="refresh">Whether to refresh an existing projection rather than subscribe.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FetchFailureProducesErrorAction(
        bool refresh
    )
    {
        await using InletSignalRActionEffectFixture fixture = new();
        InvalidOperationException failure = new("Projection request failed.");
        fixture.Fetcher
            .Setup(fetcher => fetcher.FetchAsync(typeof(TestProjection), "account-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        IAction request = refresh
            ? new RefreshProjectionAction<TestProjection>("account-1")
            : new SubscribeToProjectionAction<TestProjection>("account-1");
        Assert.Collection(
            await fixture.RunAsync(request),
            action => Assert.IsType<ProjectionLoadingAction<TestProjection>>(action),
            action => Assert.Same(failure, Assert.IsType<ProjectionErrorAction<TestProjection>>(action).Error));
    }

    /// <summary>
    ///     A valid but empty projection produces a successful null payload for both initial loads and refreshes.
    /// </summary>
    /// <param name="refresh">Whether to refresh an existing projection rather than subscribe.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingProjectionProducesSuccessfulEmptyState(
        bool refresh
    )
    {
        await using InletSignalRActionEffectFixture fixture = new();
        fixture.Fetcher
            .Setup(fetcher => fetcher.FetchAsync(typeof(TestProjection), "account-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        IAction request = refresh
            ? new RefreshProjectionAction<TestProjection>("account-1")
            : new SubscribeToProjectionAction<TestProjection>("account-1");
        Assert.Collection(
            await fixture.RunAsync(request),
            action => Assert.IsType<ProjectionLoadingAction<TestProjection>>(action),
            action =>
            {
                if (refresh)
                {
                    ProjectionUpdatedAction<TestProjection> updated =
                        Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action);
                    Assert.Null(updated.Data);
                    Assert.Equal(0, updated.Version);
                }
                else
                {
                    ProjectionLoadedAction<TestProjection> loaded =
                        Assert.IsType<ProjectionLoadedAction<TestProjection>>(action);
                    Assert.Null(loaded.Data);
                    Assert.Equal(0, loaded.Version);
                }
            });
    }

    /// <summary>
    ///     Version notifications fetch immutable data and dispatch an activity timestamp before the update.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task NotificationFetchesAnnouncedVersionForActiveSubscription()
    {
        await using InletSignalRActionEffectFixture fixture = new();
        await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1"));
        TestProjection projection = new()
        {
            Name = "version-seven",
        };
        fixture.Fetcher
            .Setup(fetcher => fetcher.FetchAtVersionAsync(
                typeof(TestProjection),
                "account-1",
                7,
                CancellationToken.None))
            .ReturnsAsync(ProjectionFetchResult.Create(projection, 99));
        await fixture.NotifyAsync("accounts", "account-1", 7);
        Assert.Collection(
            fixture.DispatchedActions,
            action => Assert.Equal(
                fixture.Time.GetUtcNow(),
                Assert.IsType<SignalRMessageReceivedAction>(action).Timestamp),
            action =>
            {
                ProjectionUpdatedAction<TestProjection> updated =
                    Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action);
                Assert.Equal("account-1", updated.EntityId);
                Assert.Equal(7, updated.Version);
                Assert.Same(projection, updated.Data);
            });
        fixture.Fetcher.Verify(
            fetcher => fetcher.FetchAtVersionAsync(typeof(TestProjection), "account-1", 7, CancellationToken.None),
            Times.Once);
    }

    /// <summary>
    ///     Notification fetch failures are surfaced while unavailable versions do not overwrite existing data.
    /// </summary>
    /// <param name="fails">Whether fetching fails rather than returning unavailable data.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NotificationHandlesMissingDataAndFetchErrors(
        bool fails
    )
    {
        await using InletSignalRActionEffectFixture fixture = new();
        await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1"));
        InvalidOperationException failure = new("Version fetch failed.");
        fixture.Fetcher
            .Setup(fetcher => fetcher.FetchAtVersionAsync(
                typeof(TestProjection),
                "account-1",
                7,
                CancellationToken.None))
            .Returns(
                fails
                    ? Task.FromException<ProjectionFetchResult?>(failure)
                    : Task.FromResult<ProjectionFetchResult?>(null));
        await fixture.NotifyAsync("accounts", "account-1", 7);
        Assert.IsType<SignalRMessageReceivedAction>(fixture.DispatchedActions[0]);
        Assert.Equal(fails ? 2 : 1, fixture.DispatchedActions.Count);
        if (fails)
        {
            Assert.Same(
                failure,
                Assert.IsType<ProjectionErrorAction<TestProjection>>(fixture.DispatchedActions[1]).Error);
        }
    }

    /// <summary>
    ///     Reconnection failures surface as projection errors instead of terminating the callback.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReconnectionFailureDispatchesProjectionError()
    {
        await using InletSignalRActionEffectFixture fixture = new();
        await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1"));
        InvalidOperationException failure = new("Resubscribe failed.");
        fixture.Connection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                CancellationToken.None))
            .ThrowsAsync(failure);
        await fixture.ReconnectAsync();
        Assert.Same(
            failure,
            Assert.IsType<ProjectionErrorAction<TestProjection>>(Assert.Single(fixture.DispatchedActions)).Error);
    }

    /// <summary>
    ///     Reconnection renews subscription IDs and refreshes data before a later unsubscribe.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReconnectionRefreshesDataAndUsesRenewedSubscriptionId()
    {
        await using InletSignalRActionEffectFixture fixture = new();
        await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1"));
        fixture.Connection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                CancellationToken.None))
            .ReturnsAsync("renewed-subscription");
        fixture.Fetcher
            .Setup(fetcher => fetcher.FetchAsync(typeof(TestProjection), "account-1", CancellationToken.None))
            .ReturnsAsync(
                ProjectionFetchResult.Create(
                    new TestProjection
                    {
                        Name = "reconnected",
                    },
                    8));
        await fixture.ReconnectAsync();
        ProjectionUpdatedAction<TestProjection> updated =
            Assert.IsType<ProjectionUpdatedAction<TestProjection>>(Assert.Single(fixture.DispatchedActions));
        Assert.Equal("reconnected", updated.Data?.Name);
        Assert.Equal(8, updated.Version);
        await fixture.RunAsync(new UnsubscribeFromProjectionAction<TestProjection>("account-1"));
        fixture.Connection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.Is<object?[]>(args => Equals(args[0], "renewed-subscription")),
                CancellationToken.None),
            Times.Once);
    }

    /// <summary>
    ///     Lost registration prevents server calls during reconnect and unsubscribe while local entries are removed.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task RemovedProjectionRegistrationPreventsReconnectAndUnsubscribeCalls()
    {
        await using InletSignalRActionEffectFixture fixture = new();
        await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1"));
        fixture.Registry.Setup(registry => registry.GetPath(typeof(TestProjection))).Returns((string?)null);
        await fixture.ReconnectAsync();
        Assert.Empty(await fixture.RunAsync(new UnsubscribeFromProjectionAction<TestProjection>("account-1")));
        await fixture.NotifyAsync("accounts", "account-1", 7);
        Assert.IsType<SignalRMessageReceivedAction>(Assert.Single(fixture.DispatchedActions));
        fixture.Connection.Verify(
            connection => connection.InvokeCoreAsync(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Subscription loads data once, forwards cancellation, and removes the server subscription on unsubscribe.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task SubscriptionLoadsOnceAndUnsubscribeAllowsResubscription()
    {
        await using InletSignalRActionEffectFixture fixture = new();
        using CancellationTokenSource cancellation = new();
        SubscribeToProjectionAction<TestProjection> subscribe = new("account-1");
        Assert.Collection(
            await fixture.RunAsync(subscribe, cancellation.Token),
            action => Assert.Equal(
                "account-1",
                Assert.IsType<ProjectionLoadingAction<TestProjection>>(action).EntityId),
            action =>
            {
                ProjectionLoadedAction<TestProjection> loaded =
                    Assert.IsType<ProjectionLoadedAction<TestProjection>>(action);
                Assert.Equal("initial", loaded.Data?.Name);
                Assert.Equal(3, loaded.Version);
                Assert.Equal("account-1", loaded.EntityId);
            });
        Assert.Empty(await fixture.RunAsync(subscribe, cancellation.Token));
        fixture.Fetcher.Verify(
            fetcher => fetcher.FetchAsync(typeof(TestProjection), "account-1", cancellation.Token),
            Times.Once);
        fixture.Connection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.Is<object?[]>(args =>
                    (args.Length == 2) && Equals(args[0], "accounts") && Equals(args[1], "account-1")),
                cancellation.Token),
            Times.Once);
        Assert.Empty(
            await fixture.RunAsync(
                new UnsubscribeFromProjectionAction<TestProjection>("account-1"),
                cancellation.Token));
        fixture.Connection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.Is<object?[]>(args =>
                    (args.Length == 3) &&
                    Equals(args[0], "subscription-1") &&
                    Equals(args[1], "accounts") &&
                    Equals(args[2], "account-1")),
                cancellation.Token),
            Times.Once);
        Assert.Equal(2, (await fixture.RunAsync(subscribe, cancellation.Token)).Count);
    }

    /// <summary>
    ///     Missing DTO registration fails without creating a server subscription.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task UnknownProjectionPathFailsBeforeCallingServer()
    {
        await using InletSignalRActionEffectFixture fixture = new();
        fixture.Registry.Setup(registry => registry.GetPath(typeof(TestProjection))).Returns((string?)null);
        IAction action = Assert.Single(
            await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1")));
        Assert.Contains(
            "No projection path registered",
            Assert.IsType<ProjectionErrorAction<TestProjection>>(action).Error.Message,
            StringComparison.Ordinal);
        fixture.Fetcher.VerifyNoOtherCalls();
        fixture.Connection.Verify(
            connection => connection.InvokeCoreAsync(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Unsubscription errors are non-fatal and the local entry is removed exactly once.
    /// </summary>
    /// <param name="cancelled">Whether the operation is cancelled rather than failing.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsubscribeFailureStillRemovesLocalSubscription(
        bool cancelled
    )
    {
        await using InletSignalRActionEffectFixture fixture = new();
        await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1"));
        Exception failure =
            cancelled ? new OperationCanceledException() : new InvalidOperationException("Hub unavailable.");
        fixture.Connection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        UnsubscribeFromProjectionAction<TestProjection> unsubscribe = new("account-1");
        Assert.Empty(await fixture.RunAsync(unsubscribe));
        Assert.Empty(await fixture.RunAsync(unsubscribe));
        fixture.Connection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(2, (await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1"))).Count);
    }

    /// <summary>
    ///     Notifications for unknown or unsubscribed projections update activity without fetching data.
    /// </summary>
    /// <param name="path">The projection path announced by the server.</param>
    /// <param name="entityId">The entity announced by the server.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData("unknown", "account-1")]
    [InlineData("accounts", "account-2")]
    public async Task UnsubscribedNotificationOnlyUpdatesActivity(
        string path,
        string entityId
    )
    {
        await using InletSignalRActionEffectFixture fixture = new();
        await fixture.RunAsync(new SubscribeToProjectionAction<TestProjection>("account-1"));
        await fixture.NotifyAsync(path, entityId, 7);
        Assert.IsType<SignalRMessageReceivedAction>(Assert.Single(fixture.DispatchedActions));
        fixture.Fetcher.Verify(
            fetcher => fetcher.FetchAtVersionAsync(
                It.IsAny<Type>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Unsupported fetchers are distinguished from valid not-found projections.
    /// </summary>
    /// <param name="refresh">Whether to refresh an existing projection rather than subscribe.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsupportedFetcherProducesConfigurationError(
        bool refresh
    )
    {
        await using InletSignalRActionEffectFixture fixture = new();
        fixture.Fetcher
            .Setup(fetcher => fetcher.FetchAsync(typeof(TestProjection), "account-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectionFetchResult?)null);
        IAction request = refresh
            ? new RefreshProjectionAction<TestProjection>("account-1")
            : new SubscribeToProjectionAction<TestProjection>("account-1");
        Assert.Collection(
            await fixture.RunAsync(request),
            action => Assert.IsType<ProjectionLoadingAction<TestProjection>>(action),
            action => Assert.Contains(
                "No fetcher registered",
                Assert.IsType<ProjectionErrorAction<TestProjection>>(action).Error.Message,
                StringComparison.Ordinal));
    }
}