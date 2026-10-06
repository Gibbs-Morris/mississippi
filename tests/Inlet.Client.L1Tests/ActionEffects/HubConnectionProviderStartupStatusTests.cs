using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Verifies the real provider's observable status after initial startup.
/// </summary>
public sealed class HubConnectionProviderStartupStatusTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private static readonly DateTimeOffset Timestamp = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static HubConnectionProvider CreateProvider(
        StartupStatusServer server,
        ConcurrentQueue<IAction> actions,
        FakeTimeProvider timeProvider
    )
    {
        Mock<IInletStore> store = new();
        store.Setup(value => value.Dispatch(It.IsAny<IAction>())).Callback<IAction>(actions.Enqueue);
        return new(
            new StartupStatusNavigationManager(server.BaseUri),
            new(() => store.Object),
            timeProvider: timeProvider);
    }

    private static SignalRConnectionState Reduce(
        IEnumerable<IAction> actions
    )
    {
        SignalRConnectionState state = new();
        foreach (IAction action in actions)
        {
            state = action switch
            {
                SignalRConnectingAction connecting => SignalRConnectionReducers.OnConnecting(state, connecting),
                SignalRConnectedAction connected => SignalRConnectionReducers.OnConnected(state, connected),
                SignalRDisconnectedAction disconnected => SignalRConnectionReducers.OnDisconnected(state, disconnected),
                var _ => state,
            };
        }

        return state;
    }

    /// <summary>
    ///     Canceling a pending initial negotiation retains cancellation and publishes Disconnected.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CanceledNegotiationPublishesDisconnected()
    {
        await using StartupStatusServer server = new();
        await server.StartAsync(TestContext.Current.CancellationToken);
        ConcurrentQueue<IAction> actions = new();
        FakeTimeProvider timeProvider = new(Timestamp);
        await using HubConnectionProvider provider = CreateProvider(server, actions, timeProvider);
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task starting = provider.EnsureConnectedAsync(cancellation.Token);
        await server.FirstNegotiation.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Assert.Equal(HubConnectionState.Connecting, provider.Connection.State);
        Assert.Equal(SignalRConnectionStatus.Connecting, Reduce(actions.ToArray()).Status);
        timeProvider.Advance(TimeSpan.FromMinutes(2));
        await cancellation.CancelAsync();
        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => starting.WaitAsync(
                Timeout,
                TestContext.Current.CancellationToken));
        Assert.True(starting.IsCanceled);
        Assert.True(cancellation.IsCancellationRequested);
        Assert.Equal(HubConnectionState.Disconnected, provider.Connection.State);
        SignalRConnectionState state = Reduce(actions.ToArray());
        Assert.Equal(SignalRConnectionStatus.Disconnected, state.Status);
        Assert.Equal(exception.Message, state.LastError);
        Assert.Equal(timeProvider.GetUtcNow(), state.LastDisconnectedAt);
        Assert.Collection(
            actions.ToArray(),
            action => Assert.IsType<SignalRConnectingAction>(action),
            action => Assert.IsType<SignalRDisconnectedAction>(action));
    }

    /// <summary>
    ///     A rejected initial negotiation publishes its terminal state and original error.
    /// </summary>
    /// <param name="statusCode">The rejected negotiation status.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(401)]
    [InlineData(503)]
    public async Task FailedNegotiationPublishesDisconnected(
        int statusCode
    )
    {
        await using StartupStatusServer server = new();
        await server.StartAsync(TestContext.Current.CancellationToken);
        ConcurrentQueue<IAction> actions = new();
        FakeTimeProvider timeProvider = new(Timestamp);
        await using HubConnectionProvider provider = CreateProvider(server, actions, timeProvider);
        int closedCount = 0;
        provider.OnClosed(_ =>
        {
            Interlocked.Increment(ref closedCount);
            return Task.CompletedTask;
        });
        Task starting = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await server.FirstNegotiation.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Assert.Equal(HubConnectionState.Connecting, provider.Connection.State);
        Assert.Equal(SignalRConnectionStatus.Connecting, Reduce(actions.ToArray()).Status);
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        server.CompleteFirstNegotiation(statusCode);
        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            starting.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Assert.Equal((HttpStatusCode)statusCode, exception.StatusCode);
        Assert.Equal(HubConnectionState.Disconnected, provider.Connection.State);
        Assert.False(provider.IsConnected);
        Assert.Equal(0, Volatile.Read(ref closedCount));
        Assert.Equal(1, server.NegotiationCount);
        SignalRConnectionState state = Reduce(actions.ToArray());
        Assert.Equal(SignalRConnectionStatus.Disconnected, state.Status);
        Assert.Equal(exception.Message, state.LastError);
        Assert.Equal(timeProvider.GetUtcNow(), state.LastDisconnectedAt);
        Assert.Null(state.LastConnectedAt);
        Assert.Null(state.ConnectionId);
        Assert.Collection(
            actions.ToArray(),
            action => Assert.IsType<SignalRConnectingAction>(action),
            action => Assert.IsType<SignalRDisconnectedAction>(action));
    }

    /// <summary>
    ///     A successful retry clears the error while retaining the failed attempt's timestamp.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task RetryAfterFailedStartupPublishesConnected()
    {
        await using StartupStatusServer server = new();
        await server.StartAsync(TestContext.Current.CancellationToken);
        ConcurrentQueue<IAction> actions = new();
        FakeTimeProvider timeProvider = new(Timestamp);
        await using HubConnectionProvider provider = CreateProvider(server, actions, timeProvider);
        Task starting = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await server.FirstNegotiation.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        server.CompleteFirstNegotiation(401);
        await Assert.ThrowsAsync<HttpRequestException>(() => starting.WaitAsync(
            Timeout,
            TestContext.Current.CancellationToken));
        timeProvider.Advance(TimeSpan.FromMinutes(4));
        await provider.EnsureConnectedAsync(TestContext.Current.CancellationToken)
            .WaitAsync(Timeout, TestContext.Current.CancellationToken);
        SignalRConnectionState state = Reduce(actions.ToArray());
        Assert.Equal(HubConnectionState.Connected, provider.Connection.State);
        Assert.Equal(SignalRConnectionStatus.Connected, state.Status);
        Assert.Equal(provider.Connection.ConnectionId, state.ConnectionId);
        Assert.Equal(timeProvider.GetUtcNow(), state.LastConnectedAt);
        Assert.Equal(Timestamp, state.LastDisconnectedAt);
        Assert.Null(state.LastError);
        Assert.Equal(2, server.NegotiationCount);
        Assert.Collection(
            actions.ToArray(),
            action => Assert.IsType<SignalRConnectingAction>(action),
            action => Assert.IsType<SignalRDisconnectedAction>(action),
            action => Assert.IsType<SignalRConnectingAction>(action),
            action => Assert.IsType<SignalRConnectedAction>(action));
    }

    /// <summary>
    ///     A successful initial startup publishes Connected and a repeated call adds no actions.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task SuccessfulStartupPublishesConnectedOnce()
    {
        await using StartupStatusServer server = new();
        await server.StartAsync(TestContext.Current.CancellationToken);
        ConcurrentQueue<IAction> actions = new();
        FakeTimeProvider timeProvider = new(Timestamp);
        await using HubConnectionProvider provider = CreateProvider(server, actions, timeProvider);
        Task starting = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await server.FirstNegotiation.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Assert.Equal(SignalRConnectionStatus.Connecting, Reduce(actions.ToArray()).Status);
        timeProvider.Advance(TimeSpan.FromMinutes(3));
        server.CompleteFirstNegotiation(200);
        await starting.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await provider.EnsureConnectedAsync(TestContext.Current.CancellationToken)
            .WaitAsync(Timeout, TestContext.Current.CancellationToken);
        SignalRConnectionState state = Reduce(actions.ToArray());
        Assert.Equal(HubConnectionState.Connected, provider.Connection.State);
        Assert.True(provider.IsConnected);
        Assert.Equal(SignalRConnectionStatus.Connected, state.Status);
        Assert.Equal(provider.Connection.ConnectionId, state.ConnectionId);
        Assert.Equal(timeProvider.GetUtcNow(), state.LastConnectedAt);
        Assert.Null(state.LastDisconnectedAt);
        Assert.Null(state.LastError);
        Assert.Equal(1, server.NegotiationCount);
        Assert.Collection(
            actions.ToArray(),
            action => Assert.IsType<SignalRConnectingAction>(action),
            action => Assert.IsType<SignalRConnectedAction>(action));
    }
}