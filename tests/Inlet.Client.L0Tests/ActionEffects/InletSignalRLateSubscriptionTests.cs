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
///     Tests release of initial projection subscriptions whose hub replies are delayed.
/// </summary>
public sealed class InletSignalRLateSubscriptionTests : IAsyncDisposable
{
    private readonly InletSignalRActionEffect effect;

    private readonly Mock<IProjectionFetcher> fetcher = new();

    private readonly Mock<HubConnection> hubConnection = new(
        Mock.Of<IConnectionFactory>(),
        new JsonHubProtocol(),
        new IPEndPoint(IPAddress.Loopback, 80),
        Mock.Of<IServiceProvider>(),
        NullLoggerFactory.Instance);

    private readonly Mock<IInletStore> store = new();

    private Func<string, string, long, Task> projectionUpdated = null!;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSignalRLateSubscriptionTests" /> class.
    /// </summary>
    public InletSignalRLateSubscriptionTests()
    {
        Mock<IHubConnectionProvider> provider = new();
        provider.SetupGet(value => value.Connection).Returns(hubConnection.Object);
        provider.Setup(value => value.EnsureConnectedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        provider.Setup(value => value.RegisterHandler(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()))
            .Callback((string _, Func<string, string, long, Task> handler) => projectionUpdated = handler)
            .Returns(Mock.Of<IDisposable>());
        hubConnection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((object?)null);
        fetcher.Setup(value => value.FetchAsync(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        fetcher.Setup(value => value.FetchAtVersionAsync(
                It.IsAny<Type>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        ProjectionDtoRegistry registry = new();
        registry.Register("test/projections", typeof(TestProjection));
        effect = new(new(() => store.Object), provider.Object, fetcher.Object, registry);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await effect.DisposeAsync();
        await hubConnection.Object.DisposeAsync();
    }

    private static async Task<IAction[]> CollectAsync(
        IAsyncEnumerable<IAction> actions
    )
    {
        List<IAction> result = [];
        await foreach (IAction action in actions)
        {
            result.Add(action);
        }

        return result.ToArray();
    }

    private TaskCompletionSource<object?> ArrangeSubscription(
        string entityId
    )
    {
        TaskCompletionSource<object?> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.Is<object?[]>(arguments => Equals(arguments[1], entityId)),
                It.IsAny<CancellationToken>()))
            .Returns(response.Task);
        return response;
    }

    private Task<IAction[]> SubscribeAsync(
        string entityId
    ) =>
        CollectAsync(
                effect.HandleAsync(
                    new SubscribeToProjectionAction<TestProjection>(entityId),
                    new(),
                    TestContext.Current.CancellationToken))
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    private Task<IAction[]> UnsubscribeAsync(
        string entityId
    ) =>
        CollectAsync(
                effect.HandleAsync(
                    new UnsubscribeFromProjectionAction<TestProjection>(entityId),
                    new(),
                    TestContext.Current.CancellationToken))
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    private void VerifyNoInitialFetch(
        string entityId
    ) =>
        fetcher.Verify(
            value => value.FetchAsync(typeof(TestProjection), entityId, It.IsAny<CancellationToken>()),
            Times.Never);

    private void VerifyNoVersionedFetch(
        string entityId
    ) =>
        fetcher.Verify(
            value => value.FetchAtVersionAsync(
                typeof(TestProjection),
                entityId,
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

    private void VerifyUnsubscribe(
        string subscriptionId,
        string entityId
    ) =>
        hubConnection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.Is<object?[]>(arguments =>
                    Equals(arguments[0], subscriptionId) &&
                    Equals(arguments[1], "test/projections") &&
                    Equals(arguments[2], entityId)),
                It.IsAny<CancellationToken>()),
            Times.Once);

    /// <summary>
    ///     A completed interest fetches its initial data and releases its owned server ID normally.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CompletedSubscriptionStillFetchesAndReleasesOwnedId()
    {
        TaskCompletionSource<object?> response = ArrangeSubscription("entity-1");
        Task<IAction[]> subscription = SubscribeAsync("entity-1");
        response.SetResult("active-id");
        IAction[] actions = await subscription;
        Assert.Equal(2, actions.Length);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(actions[0]);
        Assert.IsType<ProjectionLoadedAction<TestProjection>>(actions[1]);
        await UnsubscribeAsync("entity-1");
        VerifyUnsubscribe("active-id", "entity-1");
        await projectionUpdated("test/projections", "entity-1", 2);
        VerifyNoVersionedFetch("entity-1");
    }

    /// <summary>
    ///     Failed and cancelled hub requests do not prevent a subsequent request from succeeding.
    /// </summary>
    /// <param name="cancelled">Whether the first hub request is cancelled.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOrCancelledSubscriptionCanBeRetried(
        bool cancelled
    )
    {
        TaskCompletionSource<object?> firstResponse = ArrangeSubscription("entity-1");
        Task<IAction[]> first = SubscribeAsync("entity-1");
        if (cancelled)
        {
            firstResponse.SetCanceled(TestContext.Current.CancellationToken);
        }
        else
        {
            firstResponse.SetException(new InvalidOperationException("Subscription failed"));
        }

        IAction[] firstActions = await first;
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(firstActions[0]);
        Assert.Equal(cancelled ? 1 : 2, firstActions.Length);
        if (!cancelled)
        {
            Assert.IsType<ProjectionErrorAction<TestProjection>>(firstActions[1]);
        }

        TaskCompletionSource<object?> retryResponse = ArrangeSubscription("entity-1");
        Task<IAction[]> retry = SubscribeAsync("entity-1");
        retryResponse.SetResult("retry-id");
        Assert.IsType<ProjectionLoadedAction<TestProjection>>((await retry)[1]);
        await UnsubscribeAsync("entity-1");
        VerifyUnsubscribe("retry-id", "entity-1");
    }

    /// <summary>
    ///     Releasing one pending entity preserves an unrelated entity on the same connection.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task PendingReleasePreservesIndependentEntity()
    {
        TaskCompletionSource<object?> releasedResponse = ArrangeSubscription("entity-1");
        TaskCompletionSource<object?> activeResponse = ArrangeSubscription("entity-2");
        Task<IAction[]> released = SubscribeAsync("entity-1");
        Task<IAction[]> active = SubscribeAsync("entity-2");
        await UnsubscribeAsync("entity-1");
        activeResponse.SetResult("independent-id");
        Assert.IsType<ProjectionLoadedAction<TestProjection>>((await active)[1]);
        releasedResponse.SetResult("released-id");
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(Assert.Single(await released));
        VerifyUnsubscribe("released-id", "entity-1");
        VerifyNoInitialFetch("entity-1");
        await projectionUpdated("test/projections", "entity-1", 2);
        VerifyNoVersionedFetch("entity-1");
        await projectionUpdated("test/projections", "entity-2", 2);
        fetcher.Verify(
            value => value.FetchAtVersionAsync(typeof(TestProjection), "entity-2", 2, It.IsAny<CancellationToken>()),
            Times.Once);
        await UnsubscribeAsync("entity-2");
        VerifyUnsubscribe("independent-id", "entity-2");
    }

    /// <summary>
    ///     A late initial reply is released and cannot fetch data after its owner unsubscribes.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task PendingReleaseUnsubscribesLateIdWithoutFetching()
    {
        TaskCompletionSource<object?> response = ArrangeSubscription("entity-1");
        Task<IAction[]> subscription = SubscribeAsync("entity-1");
        Assert.False(subscription.IsCompleted);
        await UnsubscribeAsync("entity-1");
        Assert.False(subscription.IsCompleted);
        response.SetResult("late-id");
        IAction[] actions = await subscription;
        VerifyUnsubscribe("late-id", "entity-1");
        VerifyNoInitialFetch("entity-1");
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(Assert.Single(actions));
        await projectionUpdated("test/projections", "entity-1", 2);
        VerifyNoVersionedFetch("entity-1");
        store.Verify(value => value.Dispatch(It.IsAny<ProjectionUpdatedAction<TestProjection>>()), Times.Never);
    }

    /// <summary>
    ///     Cleanup of an older reply preserves the newer interest for the same projection and entity.
    /// </summary>
    /// <param name="completeNewFirst">Whether the newer reply completes before the released reply.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReleasedReplyPreservesNewerSamePairInterest(
        bool completeNewFirst
    )
    {
        TaskCompletionSource<object?> oldResponse = ArrangeSubscription("entity-1");
        Task<IAction[]> oldSubscription = SubscribeAsync("entity-1");
        await UnsubscribeAsync("entity-1");
        TaskCompletionSource<object?> newResponse = ArrangeSubscription("entity-1");
        Task<IAction[]> newSubscription = SubscribeAsync("entity-1");
        if (completeNewFirst)
        {
            newResponse.SetResult("new-id");
            Assert.IsType<ProjectionLoadedAction<TestProjection>>((await newSubscription)[1]);
        }

        oldResponse.SetResult("old-id");
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(Assert.Single(await oldSubscription));
        VerifyUnsubscribe("old-id", "entity-1");
        if (!completeNewFirst)
        {
            newResponse.SetResult("new-id");
            Assert.IsType<ProjectionLoadedAction<TestProjection>>((await newSubscription)[1]);
        }

        await projectionUpdated("test/projections", "entity-1", 3);
        fetcher.Verify(
            value => value.FetchAtVersionAsync(typeof(TestProjection), "entity-1", 3, It.IsAny<CancellationToken>()),
            Times.Once);
        await UnsubscribeAsync("entity-1");
        VerifyUnsubscribe("new-id", "entity-1");
    }

    /// <summary>
    ///     Releasing an unknown interest does not unsubscribe or prevent a later first request.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task UnknownReleaseAllowsLaterSubscription()
    {
        await UnsubscribeAsync("entity-1");
        hubConnection.Verify(
            connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        TaskCompletionSource<object?> response = ArrangeSubscription("entity-1");
        Task<IAction[]> subscription = SubscribeAsync("entity-1");
        response.SetResult("first-id");
        Assert.IsType<ProjectionLoadedAction<TestProjection>>((await subscription)[1]);
        await UnsubscribeAsync("entity-1");
        VerifyUnsubscribe("first-id", "entity-1");
    }
}