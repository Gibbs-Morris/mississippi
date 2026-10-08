using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Core;

using Moq;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Tests subscription ownership through serialized store dispatches.
/// </summary>
public sealed class InletSignalRSubscriptionTests : IAsyncDisposable
{
    private readonly Mock<HubConnection> hubConnection = new(
        Mock.Of<IConnectionFactory>(),
        new JsonHubProtocol(),
        new IPEndPoint(IPAddress.Loopback, 80),
        Mock.Of<IServiceProvider>(),
        NullLoggerFactory.Instance);

    private readonly ConcurrentDictionary<string, byte> liveSubscriptions = new();

    private readonly Channel<IAction> results = Channel.CreateUnbounded<IAction>();

    private readonly ServiceProvider serviceProvider;

    private readonly IInletStore store;

    private readonly Channel<TaskCompletionSource<object?>> subscriptionRequests =
        Channel.CreateUnbounded<TaskCompletionSource<object?>>();

    private readonly ConcurrentQueue<TaskCompletionSource<object?>> subscriptions = new();

    private readonly ConcurrentQueue<string> unsubscribedIds = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSignalRSubscriptionTests" /> class.
    /// </summary>
    public InletSignalRSubscriptionTests()
    {
        hubConnection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.SubscribeMethod,
                typeof(string),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                TaskCompletionSource<object?> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
                subscriptions.Enqueue(response);
                subscriptionRequests.Writer.TryWrite(response);
                return response.Task;
            });
        hubConnection.Setup(connection => connection.InvokeCoreAsync(
                InletHubConstants.UnsubscribeMethod,
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Type _, object?[] arguments, CancellationToken _) =>
            {
                string id = Assert.IsType<string>(arguments[0]);
                unsubscribedIds.Enqueue(id);
                liveSubscriptions.TryRemove(id, out byte _);
                return Task.FromResult<object?>(null);
            });
        Mock<IHubConnectionProvider> provider = new();
        provider.SetupGet(value => value.Connection).Returns(hubConnection.Object);
        provider.Setup(value => value.EnsureConnectedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        provider.Setup(value => value.RegisterHandler(It.IsAny<string>(), It.IsAny<Func<string, string, long, Task>>()))
            .Returns(Mock.Of<IDisposable>());
        Mock<IProjectionFetcher> fetcher = new();
        fetcher.Setup(value => value.FetchAsync(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFetchResult.NotFound);
        ProjectionDtoRegistry registry = new();
        registry.Register("test/projections", typeof(TestProjection));
        registry.Register("test/alternate", typeof(AlternateTestProjection));
        Mock<IMiddleware> middleware = new();
        middleware.Setup(value => value.Invoke(It.IsAny<IAction>(), It.IsAny<Action<IAction>>()))
            .Callback((IAction action, Action<IAction> next) =>
            {
                next(action);
                if (action is ProjectionLoadedAction<TestProjection> or ProjectionLoadedAction<AlternateTestProjection>
                    or ProjectionErrorAction<TestProjection>)
                {
                    results.Writer.TryWrite(action);
                }
            });
        ServiceCollection services = new();
        services.AddSingleton(provider.Object);
        services.AddSingleton(fetcher.Object);
        services.AddSingleton<IProjectionDtoRegistry>(registry);
        services.AddSingleton(middleware.Object);
        services.AddReservoir()
            .AddInletBlazorSignalR()
            .AddFeatureState<ProjectionsFeatureState>(feature => feature
                .AddReducer<ProjectionLoadingAction<TestProjection>>(ProjectionsReducer.ReduceLoading)
                .AddReducer<ProjectionLoadedAction<TestProjection>>(ProjectionsReducer.ReduceLoaded)
                .AddReducer<ProjectionErrorAction<TestProjection>>(ProjectionsReducer.ReduceError));
        serviceProvider = services.BuildServiceProvider();
        store = serviceProvider.GetRequiredService<IInletStore>();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        store.Dispose();
        await serviceProvider.DisposeAsync();
        await hubConnection.Object.DisposeAsync();
    }

    /// <summary>
    ///     Enumerates an effect directly so cancellation can be controlled by the test.
    /// </summary>
    /// <param name="actions">The effect actions to collect.</param>
    /// <returns>The actions emitted before the effect completes.</returns>
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

    /// <summary>
    ///     Completes each pending hub response and waits for its loaded action.
    /// </summary>
    /// <param name="reverseCompletion">Whether to complete the queued responses in reverse order.</param>
    /// <returns>A task representing completion of the pending subscriptions.</returns>
    private async Task CompleteSubscriptionsAsync(
        bool reverseCompletion = false
    )
    {
        TaskCompletionSource<object?>[] responses = subscriptions.ToArray();
        int[] indexes = Enumerable.Range(0, responses.Length)
            .Where(index => !responses[index].Task.IsCompleted)
            .ToArray();
        if (reverseCompletion)
        {
            Array.Reverse(indexes);
        }

        foreach (int index in indexes)
        {
            string id = $"subscription-{index + 1}";
            Assert.True(liveSubscriptions.TryAdd(id, 0));
            responses[index].SetResult(id);
            IAction action = await ReadResultAsync();
            Assert.True(
                action is ProjectionLoadedAction<TestProjection> or ProjectionLoadedAction<AlternateTestProjection>);
        }
    }

    /// <summary>Completes an unexpected retry so the regression reports actions and IDs instead of timing out.</summary>
    /// <param name="duplicate">The held duplicate operation.</param>
    /// <param name="context">The context holding its failed-attempt continuation.</param>
    /// <returns>The duplicate's emitted actions.</returns>
    private async Task<IAction[]> FinishHeldDuplicateAsync(
        Task<IAction[]> duplicate,
        HeldSubscriptionContinuationContext context
    )
    {
        using CancellationTokenSource nextRequestCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task<TaskCompletionSource<object?>> nextRequest =
            subscriptionRequests.Reader.ReadAsync(nextRequestCancellation.Token).AsTask();
        context.Resume();
        Task completed = await Task.WhenAny(duplicate, nextRequest)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        if (completed == nextRequest)
        {
            Assert.True(liveSubscriptions.TryAdd("unexpected-retry", 0));
            (await nextRequest).SetResult("unexpected-retry");
        }
        else
        {
            await nextRequestCancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                nextRequest.WaitAsync(TestContext.Current.CancellationToken));
        }

        return await duplicate.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Waits for a loaded or error action with a watchdog for stalled effects.
    /// </summary>
    /// <returns>The next effect result observed by the store middleware.</returns>
    private async Task<IAction> ReadResultAsync() =>
        await results.Reader.ReadAsync(TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    /// <summary>
    ///     Waits for the next hub invocation without relying on continuation scheduling.
    /// </summary>
    /// <returns>The controlled response for the next subscription request.</returns>
    private async Task<TaskCompletionSource<object?>> ReadSubscriptionRequestAsync() =>
        await subscriptionRequests.Reader.ReadAsync(TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    /// <summary>
    ///     Cancelling a waiting duplicate leaves the owner's pending request intact.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CancelledDuplicateLeavesOwnerPending()
    {
        await using InletSignalRActionEffect effect = new(
            new(() => store),
            serviceProvider.GetRequiredService<IHubConnectionProvider>(),
            serviceProvider.GetRequiredService<IProjectionFetcher>(),
            serviceProvider.GetRequiredService<IProjectionDtoRegistry>());
        SubscribeToProjectionAction<TestProjection> action = new("entity-1");
        Task<IAction[]> first = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        using CancellationTokenSource cancellation = new();
        Task<IAction[]> duplicate = CollectAsync(effect.HandleAsync(action, new(), cancellation.Token));
        await cancellation.CancelAsync();
        Assert.Empty(await duplicate);
        Assert.False(first.IsCompleted);
        TaskCompletionSource<object?> response = await ReadSubscriptionRequestAsync();
        response.SetResult("owner-subscription");
        Assert.IsType<ProjectionLoadedAction<TestProjection>>((await first)[1]);
        Assert.Single(subscriptions);
        await CollectAsync(
            effect.HandleAsync(
                new UnsubscribeFromProjectionAction<TestProjection>("entity-1"),
                new(),
                CancellationToken.None));
        Assert.Equal("owner-subscription", Assert.Single(unsubscribedIds));
    }

    /// <summary>A cancelled waiter cannot reserve a retry after observing an already-failed owner.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CancelledFailedHandoffDoesNotRetry()
    {
        await using InletSignalRActionEffect effect = new(
            new(() => store),
            serviceProvider.GetRequiredService<IHubConnectionProvider>(),
            serviceProvider.GetRequiredService<IProjectionFetcher>(),
            serviceProvider.GetRequiredService<IProjectionDtoRegistry>());
        SubscribeToProjectionAction<TestProjection> action = new("entity-1");
        Task<IAction[]> first = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        TaskCompletionSource<object?> response = await ReadSubscriptionRequestAsync();
        using CancellationTokenSource cancellation = new();
        HeldSubscriptionContinuationContext context = new();
        Task<IAction[]> duplicate =
            context.Run(() => CollectAsync(effect.HandleAsync(action, new(), cancellation.Token)));
        response.SetException(new InvalidOperationException("Subscription failed"));
        Assert.IsType<ProjectionErrorAction<TestProjection>>((await first)[1]);
        await context.WaitForContinuationAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        IAction[] actions = await FinishHeldDuplicateAsync(duplicate, context);
        Assert.Empty(actions);
        Assert.Single(subscriptions);
        Assert.Empty(liveSubscriptions);
    }

    /// <summary>
    ///     A cancelled hub request releases the pair so a later subscribe can succeed.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CancelledSubscriptionCanBeRetried()
    {
        await using InletSignalRActionEffect effect = new(
            new(() => store),
            serviceProvider.GetRequiredService<IHubConnectionProvider>(),
            serviceProvider.GetRequiredService<IProjectionFetcher>(),
            serviceProvider.GetRequiredService<IProjectionDtoRegistry>());
        using CancellationTokenSource cancellation = new();
        SubscribeToProjectionAction<TestProjection> action = new("entity-1");
        Task<IAction[]> cancelled = CollectAsync(effect.HandleAsync(action, new(), cancellation.Token));
        await cancellation.CancelAsync();
        Assert.Single(subscriptions).SetCanceled(cancellation.Token);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(Assert.Single(await cancelled));
        Task<IAction[]> retried = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        Assert.Equal(2, subscriptions.Count);
        subscriptions.Last().SetResult("retry-subscription");
        IAction[] actions = await retried;
        Assert.Equal(2, actions.Length);
        Assert.IsType<ProjectionLoadedAction<TestProjection>>(actions[1]);
        await CollectAsync(
            effect.HandleAsync(
                new UnsubscribeFromProjectionAction<TestProjection>("entity-1"),
                new(),
                CancellationToken.None));
        Assert.Equal("retry-subscription", Assert.Single(unsubscribedIds));
    }

    /// <summary>
    ///     Repeating an established subscription retains its original server ID.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CompletedSubscriptionIsNotRepeated()
    {
        store.Dispatch(new SubscribeToProjectionAction<TestProjection>("entity-1"));
        await CompleteSubscriptionsAsync();
        store.Dispatch(new SubscribeToProjectionAction<TestProjection>("entity-1"));
        store.Dispatch(new UnsubscribeFromProjectionAction<TestProjection>("entity-1"));
        Assert.Single(subscriptions);
        Assert.Equal("subscription-1", Assert.Single(unsubscribedIds));
        Assert.Empty(liveSubscriptions);
    }

    /// <summary>A failed attempt publishes its error before a new retry can replace the projection state.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task FailedAttemptPublishesBeforeRetry()
    {
        await using InletSignalRActionEffect effect = new(
            new(() => store),
            serviceProvider.GetRequiredService<IHubConnectionProvider>(),
            serviceProvider.GetRequiredService<IProjectionFetcher>(),
            serviceProvider.GetRequiredService<IProjectionDtoRegistry>());
        SubscribeToProjectionAction<TestProjection> action = new("entity-1");
        await using IAsyncEnumerator<IAction> owner = effect.HandleAsync(action, new(), CancellationToken.None)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await owner.MoveNextAsync());
        store.Dispatch(owner.Current);
        Assert.True(store.GetState<ProjectionsFeatureState>().IsProjectionLoading<TestProjection>("entity-1"));
        Task<bool> failure = owner.MoveNextAsync().AsTask();
        TaskCompletionSource<object?> response = await ReadSubscriptionRequestAsync();
        response.SetException(new InvalidOperationException("Subscription failed"));
        Assert.True(await failure.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        IAction failedAction = Assert.IsType<ProjectionErrorAction<TestProjection>>(owner.Current);

        // Initiate a new intent while delivery of the older error action is paused.
        Task<IAction[]> duplicate = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        bool retryStartedBeforeError = subscriptionRequests.Reader.TryRead(out TaskCompletionSource<object?>? retry);
        if (!retryStartedBeforeError)
        {
            store.Dispatch(failedAction);
            Assert.False(await owner.MoveNextAsync());
            retry = await ReadSubscriptionRequestAsync();
        }

        Assert.True(liveSubscriptions.TryAdd("retry-subscription", 0));
        Assert.IsType<TaskCompletionSource<object?>>(retry).SetResult("retry-subscription");
        foreach (IAction retryAction in await duplicate.WaitAsync(
                     TimeSpan.FromSeconds(10),
                     TestContext.Current.CancellationToken))
        {
            store.Dispatch(retryAction);
        }

        if (retryStartedBeforeError)
        {
            // Deliver the older error after the ready retry, as an asynchronous dispatcher may do.
            store.Dispatch(failedAction);
            Assert.False(await owner.MoveNextAsync());
        }

        ProjectionsFeatureState state = store.GetState<ProjectionsFeatureState>();
        await CollectAsync(
            effect.HandleAsync(
                new UnsubscribeFromProjectionAction<TestProjection>("entity-1"),
                new(),
                CancellationToken.None));
        Assert.Equal("retry-subscription", Assert.Single(unsubscribedIds));
        Assert.Empty(liveSubscriptions);
        Assert.False(state.IsProjectionLoading<TestProjection>("entity-1"));
        Assert.Null(state.GetProjectionError<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     A failed hub request releases the pair so a later dispatch can retry.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task FailedSubscriptionCanBeRetried()
    {
        store.Dispatch(new SubscribeToProjectionAction<TestProjection>("entity-1"));
        Assert.Single(subscriptions).SetException(new InvalidOperationException("Subscription failed"));
        IAction failure = await ReadResultAsync();
        ProjectionErrorAction<TestProjection> error = Assert.IsType<ProjectionErrorAction<TestProjection>>(failure);
        Assert.IsType<InvalidOperationException>(error.Error);
        store.Dispatch(new SubscribeToProjectionAction<TestProjection>("entity-1"));
        await CompleteSubscriptionsAsync();
        store.Dispatch(new UnsubscribeFromProjectionAction<TestProjection>("entity-1"));
        Assert.Equal(2, subscriptions.Count);
        Assert.Equal("subscription-2", Assert.Single(unsubscribedIds));
        Assert.Empty(liveSubscriptions);
    }

    /// <summary>
    ///     Independent entities and DTO types can subscribe while another pair is pending.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task IndependentPairsSubscribeWhileFirstPairIsPending()
    {
        store.Dispatch(new SubscribeToProjectionAction<TestProjection>("entity-1"));
        store.Dispatch(new SubscribeToProjectionAction<TestProjection>("entity-2"));
        store.Dispatch(new SubscribeToProjectionAction<AlternateTestProjection>("entity-1"));
        await CompleteSubscriptionsAsync();
        store.Dispatch(new UnsubscribeFromProjectionAction<TestProjection>("entity-1"));
        store.Dispatch(new UnsubscribeFromProjectionAction<TestProjection>("entity-2"));
        store.Dispatch(new UnsubscribeFromProjectionAction<AlternateTestProjection>("entity-1"));
        Assert.Equal(3, subscriptions.Count);
        Assert.Equal(3, unsubscribedIds.Count);
        Assert.Empty(liveSubscriptions);
    }

    /// <summary>
    ///     Repeating a pending same-pair request leaves no server subscription after unsubscribe.
    /// </summary>
    /// <param name="reverseCompletion">Whether hub responses complete in reverse invocation order.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OverlappingSamePairRequestsLeaveNoUnownedSubscription(
        bool reverseCompletion
    )
    {
        store.Dispatch(new SubscribeToProjectionAction<TestProjection>("entity-1"));
        store.Dispatch(new SubscribeToProjectionAction<TestProjection>("entity-1"));
        await CompleteSubscriptionsAsync(reverseCompletion);
        store.Dispatch(new UnsubscribeFromProjectionAction<TestProjection>("entity-1"));
        Assert.Empty(liveSubscriptions);
        Assert.Single(subscriptions);
        Assert.Equal("subscription-1", Assert.Single(unsubscribedIds));
    }

    /// <summary>
    ///     A waiting duplicate retains its intent after the first attempt fails or is cancelled.
    /// </summary>
    /// <param name="cancelFirst">Whether the first attempt is cancelled instead of failing.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingDuplicateRetriesAfterFailedAttempt(
        bool cancelFirst
    )
    {
        await using InletSignalRActionEffect effect = new(
            new(() => store),
            serviceProvider.GetRequiredService<IHubConnectionProvider>(),
            serviceProvider.GetRequiredService<IProjectionFetcher>(),
            serviceProvider.GetRequiredService<IProjectionDtoRegistry>());
        using CancellationTokenSource cancellation = new();
        SubscribeToProjectionAction<TestProjection> action = new("entity-1");
        Task<IAction[]> first = CollectAsync(effect.HandleAsync(action, new(), cancellation.Token));
        Task<IAction[]> duplicate = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        TaskCompletionSource<object?> firstResponse = await ReadSubscriptionRequestAsync();
        if (cancelFirst)
        {
            await cancellation.CancelAsync();
            firstResponse.SetCanceled(cancellation.Token);
        }
        else
        {
            firstResponse.SetException(new InvalidOperationException("Subscription failed"));
        }

        IAction[] firstActions = await first;
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(firstActions[0]);
        Assert.Equal(cancelFirst ? 1 : 2, firstActions.Length);
        Assert.False(duplicate.IsCompleted);
        TaskCompletionSource<object?> retryResponse = await ReadSubscriptionRequestAsync();
        retryResponse.SetResult("retry-subscription");
        IAction[] duplicateActions = await duplicate;
        Assert.Equal(2, duplicateActions.Length);
        Assert.IsType<ProjectionLoadedAction<TestProjection>>(duplicateActions[1]);
        Assert.Equal(2, subscriptions.Count);
        await CollectAsync(
            effect.HandleAsync(
                new UnsubscribeFromProjectionAction<TestProjection>("entity-1"),
                new(),
                CancellationToken.None));
        Assert.Equal("retry-subscription", Assert.Single(unsubscribedIds));
    }

    /// <summary>Release after a failed owner retires a waiter before it takes retry ownership.</summary>
    /// <param name="disposeEffect">Whether disposal releases interest instead of unsubscribe.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReleasedFailedHandoffDoesNotRestart(
        bool disposeEffect
    )
    {
        await using InletSignalRActionEffect effect = new(
            new(() => store),
            serviceProvider.GetRequiredService<IHubConnectionProvider>(),
            serviceProvider.GetRequiredService<IProjectionFetcher>(),
            serviceProvider.GetRequiredService<IProjectionDtoRegistry>());
        SubscribeToProjectionAction<TestProjection> action = new("entity-1");
        Task<IAction[]> first = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        TaskCompletionSource<object?> response = await ReadSubscriptionRequestAsync();
        HeldSubscriptionContinuationContext context = new();
        Task<IAction[]> duplicate =
            context.Run(() => CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None)));
        response.SetException(new InvalidOperationException("Subscription failed"));
        Assert.IsType<ProjectionErrorAction<TestProjection>>((await first)[1]);
        await context.WaitForContinuationAsync(TestContext.Current.CancellationToken);
        if (disposeEffect)
        {
            await effect.DisposeAsync();
        }
        else
        {
            await CollectAsync(
                effect.HandleAsync(
                    new UnsubscribeFromProjectionAction<TestProjection>("entity-1"),
                    new(),
                    CancellationToken.None));
        }

        IAction[] actions = await FinishHeldDuplicateAsync(duplicate, context);
        Assert.Empty(actions);
        Assert.Single(subscriptions);
        Assert.Empty(liveSubscriptions);
    }

    /// <summary>
    ///     Releasing pending interest also retires a waiting duplicate without restarting it.
    /// </summary>
    /// <param name="disposeEffect">Whether disposal releases interest instead of unsubscribe.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReleasedPendingDuplicateDoesNotRestart(
        bool disposeEffect
    )
    {
        await using InletSignalRActionEffect effect = new(
            new(() => store),
            serviceProvider.GetRequiredService<IHubConnectionProvider>(),
            serviceProvider.GetRequiredService<IProjectionFetcher>(),
            serviceProvider.GetRequiredService<IProjectionDtoRegistry>());
        SubscribeToProjectionAction<TestProjection> action = new("entity-1");
        Task<IAction[]> first = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        Task<IAction[]> duplicate = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        TaskCompletionSource<object?> response = await ReadSubscriptionRequestAsync();
        if (disposeEffect)
        {
            await effect.DisposeAsync();
        }
        else
        {
            await CollectAsync(
                effect.HandleAsync(
                    new UnsubscribeFromProjectionAction<TestProjection>("entity-1"),
                    new(),
                    CancellationToken.None));
        }

        Assert.Empty(await duplicate.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.False(first.IsCompleted);
        Assert.True(liveSubscriptions.TryAdd("released-subscription", 0));
        response.SetResult("released-subscription");
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(Assert.Single(await first));
        Assert.Single(subscriptions);
        Assert.Equal("released-subscription", Assert.Single(unsubscribedIds));
        Assert.Empty(liveSubscriptions);
    }

    /// <summary>
    ///     A duplicate coalesced with success cannot establish a new ID after unsubscribe.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task SuccessfulPendingDuplicateDoesNotResubscribeAfterUnsubscribe()
    {
        await using InletSignalRActionEffect effect = new(
            new(() => store),
            serviceProvider.GetRequiredService<IHubConnectionProvider>(),
            serviceProvider.GetRequiredService<IProjectionFetcher>(),
            serviceProvider.GetRequiredService<IProjectionDtoRegistry>());
        SubscribeToProjectionAction<TestProjection> action = new("entity-1");
        Task<IAction[]> first = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        Task<IAction[]> duplicate = CollectAsync(effect.HandleAsync(action, new(), CancellationToken.None));
        TaskCompletionSource<object?> response = await ReadSubscriptionRequestAsync();
        response.SetResult("owner-subscription");
        Assert.IsType<ProjectionLoadedAction<TestProjection>>((await first)[1]);
        await CollectAsync(
            effect.HandleAsync(
                new UnsubscribeFromProjectionAction<TestProjection>("entity-1"),
                new(),
                CancellationToken.None));
        Assert.Empty(await duplicate);
        Assert.Single(subscriptions);
        Assert.Equal("owner-subscription", Assert.Single(unsubscribedIds));
    }
}