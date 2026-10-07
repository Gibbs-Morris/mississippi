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

    private static async Task WaitForRetryToStartAsync(
        TaskCompletionSource<bool> retryStarted,
        Task operation
    )
    {
        Task timeout = Task.Delay(TimeSpan.FromSeconds(5));
#pragma warning disable VSTHRD003 // The operation was started by this test and is observed to bound the coordination gate.
        Task completed = await Task.WhenAny(retryStarted.Task, operation, timeout);
#pragma warning restore VSTHRD003
        if (ReferenceEquals(completed, operation))
        {
            throw new InvalidOperationException(
                "The retry operation completed before starting a hub subscribe request.");
        }

        if (ReferenceEquals(completed, timeout))
        {
            throw new TimeoutException(
                "The retry operation did not start a hub subscribe request within five seconds.");
        }
    }

    private void ConfigureFailedRetryWithPendingReply(
        TaskCompletionSource<object?> retryReply,
        TaskCompletionSource<bool> retryStarted
    )
    {
        int invocationCount = 0;
        hubConnection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Type _, object?[] _, CancellationToken _) =>
            {
                int invocation = Interlocked.Increment(ref invocationCount);
                if (invocation == 1)
                {
                    return Task.FromResult<object?>("old-id");
                }

                if (invocation == 2)
                {
                    return Task.FromException<object?>(new InvalidOperationException("Reconnect subscription failed"));
                }

                if (invocation == 3)
                {
                    retryStarted.TrySetResult(true);
#pragma warning disable VSTHRD003 // The retry reply is deliberately released by the test's completion-source gate.
                    return retryReply.Task;
#pragma warning restore VSTHRD003
                }

                return Task.FromResult<object?>("unexpected-id-" + invocation);
            });
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
    ///     An explicit failed-interest retry prevents a concurrent reconnect from creating a second server subscription.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ExplicitRetryClaimsFailedInterestAgainstConcurrentReconnect()
    {
        TaskCompletionSource<object?> retryReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> retryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConfigureFailedRetryWithPendingReply(retryReply, retryStarted);
        await FailFirstReconnectAsync();
        Task<IAction[]> retryTask = HandleAsync(new SubscribeToProjectionAction<TestProjection>(EntityId));
        await WaitForRetryToStartAsync(retryStarted, retryTask);
        await reconnected("overlapping-reconnect");
        VerifySubscribeCount(3);
        retryReply.SetResult("recovered-id");
        IAction[] retryActions = await retryTask;
        Assert.Collection(
            retryActions,
            action => Assert.IsType<ProjectionLoadingAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionLoadedAction<TestProjection>>(action));
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("recovered-id");
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
    ///     A reconnect reply arriving after unsubscribe is cleaned up without restoring the released interest.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task LateReconnectReplyAfterUnsubscribeIsDiscardedAndCleanedUp()
    {
        TaskCompletionSource<object?> retryReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> retryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConfigureFailedRetryWithPendingReply(retryReply, retryStarted);
        await FailFirstReconnectAsync();
        Task reconnectTask = reconnected("retry-before-unsubscribe");
        await WaitForRetryToStartAsync(retryStarted, reconnectTask);
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
    ///     A failed-interest reconnect is claimed while its hub reply is pending.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ReconnectClaimsFailedInterestAgainstConcurrentReconnect()
    {
        TaskCompletionSource<object?> retryReply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> retryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConfigureFailedRetryWithPendingReply(retryReply, retryStarted);
        await FailFirstReconnectAsync();
        Task firstReconnect = reconnected("first-retry");
        await WaitForRetryToStartAsync(retryStarted, firstReconnect);
        await reconnected("second-retry");
        VerifySubscribeCount(3);
        retryReply.SetResult("recovered-id");
        await firstReconnect;
        Assert.Collection(
            dispatchedActions,
            action => Assert.IsType<ProjectionErrorAction<TestProjection>>(action),
            action => Assert.IsType<ProjectionUpdatedAction<TestProjection>>(action));
        await HandleAsync(new UnsubscribeFromProjectionAction<TestProjection>(EntityId));
        VerifyUnsubscribedId("recovered-id");
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