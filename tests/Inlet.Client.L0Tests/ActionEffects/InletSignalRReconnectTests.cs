using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Tests recovery of connection-scoped projection subscription IDs.
/// </summary>
public sealed class InletSignalRReconnectTests : IAsyncDisposable
{
    private const string EntityId = "entity-1";

    private const string ProjectionPath = "test/projections";

    private readonly List<IAction> dispatchedActions = [];

    private readonly InletSignalRActionEffect effect;

    private readonly Mock<HubConnection> hubConnection = new(
        Mock.Of<IConnectionFactory>(),
        new JsonHubProtocol(),
        new IPEndPoint(IPAddress.Loopback, 80),
        Mock.Of<IServiceProvider>(),
        NullLoggerFactory.Instance);

    private readonly Mock<IProjectionFetcher> projectionFetcher = new();

    private Func<string, string, long, Task> projectionUpdated = (_, _, _) => Task.CompletedTask;

    private Func<string?, Task> reconnected = _ => Task.CompletedTask;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSignalRReconnectTests" /> class.
    /// </summary>
    public InletSignalRReconnectTests()
    {
        Mock<IHubConnectionProvider> provider = new();
        provider.SetupGet(value => value.Connection).Returns(hubConnection.Object);
        provider.Setup(value => value.EnsureConnectedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        provider.Setup(value => value.RegisterHandler(It.IsAny<string>(), It.IsAny<Func<string, string, long, Task>>()))
            .Callback((string _, Func<string, string, long, Task> handler) => projectionUpdated = handler)
            .Returns(Mock.Of<IDisposable>());
        provider.Setup(value => value.OnReconnected(It.IsAny<Func<string?, Task>>()))
            .Callback<Func<string?, Task>>(handler => reconnected = handler);
        projectionFetcher.Setup(value => value.FetchAsync(
                typeof(TestProjection),
                EntityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        Mock<IInletStore> store = new();
        store.Setup(value => value.Dispatch(It.IsAny<IAction>())).Callback<IAction>(dispatchedActions.Add);
        ProjectionDtoRegistry registry = new();
        registry.Register(ProjectionPath, typeof(TestProjection));
        effect = new(new(() => store.Object), provider.Object, projectionFetcher.Object, registry);
        hubConnection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((object?)null);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await effect.DisposeAsync();
        await hubConnection.Object.DisposeAsync();
    }

    private TaskCompletionSource<object?> ConfigureFailedRetryWithPendingReply()
    {
        TaskCompletionSource<object?> retryReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-id")
            .ThrowsAsync(new InvalidOperationException("Reconnect subscription failed"))
            .Returns(retryReply.Task)
            .ReturnsAsync("current-id");
        return retryReply;
    }

    private async Task FailFirstReconnectAsync()
    {
        await SubscribeAsync();
        await reconnected("failed-connection");
        Assert.IsType<ProjectionErrorAction<TestProjection>>(Assert.Single(dispatchedActions));
    }

    private async Task<IAction[]> HandleAsync(
        IAction action
    )
    {
        List<IAction> results = [];
        await foreach (IAction result in effect.HandleAsync(action, new(), CancellationToken.None))
        {
            results.Add(result);
        }

        return results.ToArray();
    }

    private async Task SubscribeAsync()
    {
        IAction[] actions = await HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId));
        Assert.Collection(
            actions,
            action => Assert.IsType<ProjectionLoadingAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionLoadedAction<TestProjection>>(action));
    }

    private void VerifySubscribeCount(
        int count
    ) =>
        hubConnection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.Is<object?[]>(arguments => (arguments.Length == 2) &&
                                              ((string?)arguments[0] == ProjectionPath) &&
                                              ((string?)arguments[1] == EntityId)),
                It.IsAny<CancellationToken>()),
            Times.Exactly(count));

    private void VerifyUnsubscribedId(
        string subscriptionId
    ) =>
        hubConnection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.Is<object?[]>(arguments => (arguments.Length == 3) &&
                                              ((string?)arguments[0] == subscriptionId) &&
                                              ((string?)arguments[1] == ProjectionPath) &&
                                              ((string?)arguments[2] == EntityId)),
                It.IsAny<CancellationToken>()),
            Times.Once);

    /// <summary>
    ///     Overlapping explicit retries of a failed interest issue only one server subscription.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ConcurrentExplicitRetriesShareOneClaim()
    {
        TaskCompletionSource<object?> reply = ConfigureFailedRetryWithPendingReply();
        await FailFirstReconnectAsync();
        Task<IAction[]> firstRetry = HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId));
        IAction[] secondRetry = await HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId));
        reply.SetResult("recovered-id");
        IAction[] firstActions = await firstRetry.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);
        Assert.Empty(secondRetry);
        Assert.Collection(
            firstActions,
            action => Assert.IsType<ProjectionLoadingAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionLoadedAction<TestProjection>>(action));
        VerifySubscribeCount(3);
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("recovered-id");
    }

    /// <summary>
    ///     A failed or cancelled explicit retry releases its claim without losing desired interest.
    /// </summary>
    /// <param name="isCancelled">Whether the explicit retry is cancelled instead of failing.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedExplicitRetryKeepsInterestRetryable(
        bool isCancelled
    )
    {
        Exception retryFailure = isCancelled
            ? new OperationCanceledException()
            : new InvalidOperationException("Explicit retry failed");
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-id")
            .ThrowsAsync(new InvalidOperationException("Reconnect subscription failed"))
            .ThrowsAsync(retryFailure)
            .ReturnsAsync("recovered-id");
        await FailFirstReconnectAsync();
        IAction[] failedActions = await HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId));
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(failedActions[0]);
        if (isCancelled)
        {
            Assert.Single(failedActions);
        }
        else
        {
            ProjectionErrorAction<TestProjection> error =
                Assert.IsType<ProjectionErrorAction<TestProjection>>(failedActions[1]);
            Assert.Same(retryFailure, error.Error);
            Assert.Equal(2, failedActions.Length);
        }

        await SubscribeAsync();
        VerifySubscribeCount(4);
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("recovered-id");
    }

    /// <summary>
    ///     Retained interest ignores notifications until a retry establishes a usable subscription ID.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task FailedInterestIgnoresNotificationsUntilRecovered()
    {
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-id")
            .ThrowsAsync(new InvalidOperationException("Reconnect subscription failed"))
            .ReturnsAsync("recovered-id");
        projectionFetcher.Setup(value => value.FetchAtVersionAsync(
                typeof(TestProjection),
                EntityId,
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        await FailFirstReconnectAsync();
        dispatchedActions.Clear();
        await projectionUpdated(ProjectionPath, EntityId, 1);
        Assert.IsType<SignalRMessageReceivedAction>(Assert.Single(dispatchedActions));
        projectionFetcher.Verify(
            value => value.FetchAtVersionAsync(
                typeof(TestProjection),
                EntityId,
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        await SubscribeAsync();
        dispatchedActions.Clear();
        await projectionUpdated(ProjectionPath, EntityId, 2);
        Assert.Collection(
            dispatchedActions,
            action => Assert.IsType<SignalRMessageReceivedAction>(action),
            action => Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action));
        projectionFetcher.Verify(
            value => value.FetchAtVersionAsync(typeof(TestProjection), EntityId, 2, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     A deliberate subscribe can recover from a failed reconnect invocation.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task FailedResubscriptionAllowsExplicitRetry()
    {
        InvalidOperationException failure = new("Reconnect subscription failed");
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-id")
            .ThrowsAsync(failure)
            .ReturnsAsync("recovered-id");
        await SubscribeAsync();
        await reconnected("new-connection");
        ProjectionErrorAction<TestProjection> error =
            Assert.IsType<ProjectionErrorAction<TestProjection>>(Assert.Single(dispatchedActions));
        Assert.Same(failure, error.Error);
        IAction[] retryActions = await HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId));
        Assert.Collection(
            retryActions,
            action => Assert.IsType<ProjectionLoadingAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionLoadedAction<TestProjection>>(action));
        VerifySubscribeCount(3);
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("recovered-id");
    }

    /// <summary>
    ///     A failed reconnect retains owner intent for the next reconnect.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task FailedResubscriptionRetainsInterestForLaterReconnect()
    {
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-id")
            .ThrowsAsync(new InvalidOperationException("Reconnect subscription failed"))
            .ReturnsAsync("recovered-id");
        await SubscribeAsync();
        await reconnected("failed-connection");
        await reconnected("recovered-connection");
        Assert.Collection(
            dispatchedActions,
            action => Assert.IsType<ProjectionErrorAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action));
        Assert.Empty(await HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId)));
        VerifySubscribeCount(3);
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("recovered-id");
    }

    /// <summary>
    ///     A failed data fetch preserves the ID established by the reconnect.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task FetchFailureAfterResubscriptionKeepsNewId()
    {
        InvalidOperationException failure = new("Projection fetch failed");
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-id")
            .ReturnsAsync("new-id");
        projectionFetcher.SetupSequence(value => value.FetchAsync(
                typeof(TestProjection),
                EntityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound)
            .ThrowsAsync(failure);
        await SubscribeAsync();
        await reconnected("new-connection");
        ProjectionErrorAction<TestProjection> error =
            Assert.IsType<ProjectionErrorAction<TestProjection>>(Assert.Single(dispatchedActions));
        Assert.Same(failure, error.Error);
        Assert.Empty(await HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId)));
        VerifySubscribeCount(2);
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("new-id");
    }

    /// <summary>
    ///     An explicit retry released before its reply cannot restore the removed interest.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task LateExplicitRetryReplyAfterUnsubscribeIsCleanedUp()
    {
        TaskCompletionSource<object?> reply = ConfigureFailedRetryWithPendingReply();
        await FailFirstReconnectAsync();
        Task<IAction[]> retry = HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId));
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        reply.SetResult("late-id");
        IAction[] retryActions = await retry.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(Assert.Single(retryActions));
        VerifyUnsubscribedId("late-id");
        await reconnected("after-unsubscribe");
        VerifySubscribeCount(3);
    }

    /// <summary>
    ///     A reconnect reply arriving after unsubscribe is cleaned up without restoring the released interest.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task LateReconnectReplyAfterUnsubscribeIsDiscardedAndCleanedUp()
    {
        TaskCompletionSource<object?> retryReply = ConfigureFailedRetryWithPendingReply();
        await FailFirstReconnectAsync();
        Task reconnectTask = reconnected("retry-before-unsubscribe");
        VerifySubscribeCount(3);
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        hubConnection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        retryReply.SetResult("late-id");
        await reconnectTask;
        VerifyUnsubscribedId("late-id");
        await reconnected("after-unsubscribe");
        VerifySubscribeCount(3);
    }

    /// <summary>
    ///     Starting a new generation rejects an older reply even before the new callback reaches that interest.
    /// </summary>
    /// <param name="oldReplyFails">Whether the older pending invocation fails instead of returning an ID.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewGenerationRetiresOlderReplyBeforeReachingItsInterest(
        bool oldReplyFails
    )
    {
        TaskCompletionSource<object?> oldReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<object?> newReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("initial-one")
            .ReturnsAsync("initial-two")
            .ReturnsAsync("older-first-id")
            .Returns(oldReply.Task)
            .Returns(newReply.Task)
            .ReturnsAsync("current-last-id");
        projectionFetcher.Setup(value => value.FetchAsync(
                typeof(TestProjection),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        await SubscribeAsync();
        IAction[] otherInitial = await HandleAsync(new SubscribeToProjectionAction<TestProjection>("entity-2"));
        Assert.IsType<ProjectionLoadedAction<TestProjection>>(otherInitial[1]);
        Task olderReconnect = reconnected("older-connection");
        Task latestReconnect = reconnected("latest-connection");
        if (oldReplyFails)
        {
            oldReply.SetException(new InvalidOperationException("Older pending interest failed"));
        }
        else
        {
            oldReply.SetResult("older-late-id");
        }

        await olderReconnect.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        int afterOlderReply = dispatchedActions.Count;
        newReply.SetResult("current-first-id");
        await latestReconnect.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(1, afterOlderReply);
        Assert.Equal(3, dispatchedActions.Count);
        Assert.All(dispatchedActions, action => Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action));
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>("entity-2"));
        foreach (string id in new[] { "current-first-id", "current-last-id" })
        {
            hubConnection.Verify(
                connection => connection.InvokeCoreAsync(
                    InletHubConstants.UnsubscribeMethod,
                    It.IsAny<Type>(),
                    It.Is<object?[]>(arguments => (string?)arguments[0] == id),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        hubConnection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(6));
    }

    /// <summary>
    ///     A superseded explicit retry cannot publish its delayed initial fetch over the newest connection's state.
    /// </summary>
    /// <param name="oldFetchFails">Whether the earlier fetch fails instead of returning projection data.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewReconnectDiscardsSupersededExplicitRetryFetch(
        bool oldFetchFails
    )
    {
        TaskCompletionSource<ProjectionFetchResult?> oldFetch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-id")
            .ThrowsAsync(new InvalidOperationException("Reconnect subscription failed"))
            .ReturnsAsync("retry-id")
            .ReturnsAsync("current-id");
        projectionFetcher
            .SetupSequence(value => value.FetchAsync(typeof(TestProjection), EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound)
            .Returns(oldFetch.Task)
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        await FailFirstReconnectAsync();
        Task<IAction[]> explicitRetry = HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId));
        VerifySubscribeCount(3);
        await reconnected("latest-connection");
        if (oldFetchFails)
        {
            oldFetch.SetException(new InvalidOperationException("Superseded retry fetch failed"));
        }
        else
        {
            oldFetch.SetResult(ProjectionFetchResult.NotFound);
        }

        IAction[] retryActions = await explicitRetry.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(Assert.Single(retryActions));
        Assert.Collection(
            dispatchedActions,
            action => Assert.IsType<ProjectionErrorAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action));
        VerifySubscribeCount(4);
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("current-id");
    }

    /// <summary>
    ///     An older connection's delayed projection fetch cannot publish after a newer reconnect has recovered.
    /// </summary>
    /// <param name="oldFetchFails">Whether the superseded fetch fails instead of returning projection data.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewReconnectDiscardsSupersededProjectionFetch(
        bool oldFetchFails
    )
    {
        TaskCompletionSource<ProjectionFetchResult?> oldFetch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("initial-id")
            .ReturnsAsync("superseded-id")
            .ReturnsAsync("current-id");
        projectionFetcher
            .SetupSequence(value => value.FetchAsync(typeof(TestProjection), EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound)
            .Returns(oldFetch.Task)
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        await SubscribeAsync();
        Task firstReconnect = reconnected("earlier-connection");
        VerifySubscribeCount(2);
        await reconnected("latest-connection");
        if (oldFetchFails)
        {
            oldFetch.SetException(new InvalidOperationException("Superseded fetch failed"));
        }
        else
        {
            oldFetch.SetResult(ProjectionFetchResult.NotFound);
        }

        await firstReconnect.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        VerifySubscribeCount(3);
        Assert.IsType<ProjectionUpdatedAction<TestProjection>>(Assert.Single(dispatchedActions));
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("current-id");
    }

    /// <summary>
    ///     A newer connection supersedes an explicit retry belonging to the disconnected connection.
    /// </summary>
    /// <param name="oldReplyFails">Whether the superseded invocation fails instead of returning an ID.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewReconnectSupersedesPendingExplicitRetry(
        bool oldReplyFails
    )
    {
        TaskCompletionSource<object?> retryReply = ConfigureFailedRetryWithPendingReply();
        await FailFirstReconnectAsync();
        Task<IAction[]> retryTask = HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId));
        VerifySubscribeCount(3);
        await reconnected(null);
        if (oldReplyFails)
        {
            retryReply.SetException(new InvalidOperationException("Superseded retry failed"));
        }
        else
        {
            retryReply.SetResult("superseded-id");
        }

        IAction[] retryActions = await retryTask.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);
        VerifySubscribeCount(4);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(Assert.Single(retryActions));
        Assert.Collection(
            dispatchedActions,
            action => Assert.IsType<ProjectionErrorAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action));
        if (!oldReplyFails)
        {
            VerifyUnsubscribedId("superseded-id");
        }

        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("current-id");
    }

    /// <summary>
    ///     The latest reconnect restores interest while the earlier connection's invocation is still pending.
    /// </summary>
    /// <param name="oldReplyFails">Whether the superseded invocation fails instead of returning an ID.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewReconnectSupersedesPendingReconnect(
        bool oldReplyFails
    )
    {
        TaskCompletionSource<object?> retryReply = ConfigureFailedRetryWithPendingReply();
        await FailFirstReconnectAsync();
        Task firstReconnect = reconnected(null);
        VerifySubscribeCount(3);
        await reconnected(null);
        if (oldReplyFails)
        {
            retryReply.SetException(new InvalidOperationException("Superseded reconnect failed"));
        }
        else
        {
            retryReply.SetResult("superseded-id");
        }

        await firstReconnect.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        VerifySubscribeCount(4);
        Assert.Collection(
            dispatchedActions,
            action => Assert.IsType<ProjectionErrorAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action));
        if (!oldReplyFails)
        {
            VerifyUnsubscribedId("superseded-id");
        }

        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("current-id");
    }

    /// <summary>
    ///     A successful reconnect replaces the old ID and skips duplicate subscriptions.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task SuccessfulResubscriptionReplacesIdAndSkipsDuplicates()
    {
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-id")
            .ReturnsAsync("new-id");
        await SubscribeAsync();
        await reconnected("new-connection");
        Assert.IsType<ProjectionUpdatedAction<TestProjection>>(Assert.Single(dispatchedActions));
        Assert.Empty(await HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId)));
        VerifySubscribeCount(2);
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("new-id");
    }

    /// <summary>
    ///     Unsubscribing a failed interest avoids using its obsolete ID and permits a new owner subscription.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task UnsubscribeAfterFailureDropsInterestWithoutUsingOldId()
    {
        hubConnection.SetupSequence(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-id")
            .ThrowsAsync(new InvalidOperationException("Reconnect subscription failed"))
            .ReturnsAsync("owner-retry-id");
        await SubscribeAsync();
        await reconnected("failed-connection");
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        hubConnection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        await reconnected("later-connection");
        VerifySubscribeCount(2);
        await SubscribeAsync();
        VerifySubscribeCount(3);
    }
}