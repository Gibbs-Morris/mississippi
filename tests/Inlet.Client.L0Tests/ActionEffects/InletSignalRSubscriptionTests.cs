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
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;
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
        services.AddReservoir().AddInletBlazorSignalR();
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