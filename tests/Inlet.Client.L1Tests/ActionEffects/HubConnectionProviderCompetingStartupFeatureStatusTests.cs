using System;
using System.Collections.Concurrent;
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
///     Verifies the feature status when a competing transport finishes startup.
/// </summary>
public sealed class HubConnectionProviderCompetingStartupFeatureStatusTests
{
    /// <summary>
    ///     A rejected caller publishes the observed competing connection before propagating its original error.
    /// </summary>
    /// <returns>A task representing the regression.</returns>
    [Fact]
    public async Task ConnectedCompetitorPublishesConnectedFeatureStatus()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(15);
        await using StartupStatusServer server = new();
        await server.StartAsync(TestContext.Current.CancellationToken);
        ConcurrentQueue<IAction> actions = new();
        Mock<IInletStore> store = new();
        FakeTimeProvider time = new(new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));
        await using HubConnectionProvider provider = new(
            new StartupStatusNavigationManager(server.BaseUri),
            new(() => store.Object),
            timeProvider: time);
        Task competingStart = Task.CompletedTask;
        store.Setup(value => value.Dispatch(It.IsAny<IAction>()))
            .Callback<IAction>(action =>
            {
                actions.Enqueue(action);
                if (action is SignalRConnectingAction)
                {
                    competingStart = provider.Connection.StartAsync(TestContext.Current.CancellationToken);
                }
            });
        Task starting = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await server.FirstNegotiation.WaitAsync(timeout, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMinutes(1));
        server.CompleteFirstNegotiation(200);
        await competingStart.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => starting.WaitAsync(
            timeout,
            TestContext.Current.CancellationToken));
        Assert.True(starting.IsFaulted);
        Assert.Equal(HubConnectionState.Connected, provider.Connection.State);
        Assert.Collection(
            actions.ToArray(),
            action => Assert.IsType<SignalRConnectingAction>(action),
            action =>
            {
                SignalRConnectedAction connected = Assert.IsType<SignalRConnectedAction>(action);
                Assert.Equal(provider.Connection.ConnectionId, connected.ConnectionId);
                Assert.Equal(time.GetUtcNow(), connected.Timestamp);
                SignalRConnectionState initial = SignalRConnectionReducers.OnConnecting(new(), new());
                SignalRConnectionState state = SignalRConnectionReducers.OnConnected(initial, connected);
                Assert.Equal(SignalRConnectionStatus.Connected, state.Status);
                Assert.Null(state.LastError);
                Assert.Equal(time.GetUtcNow(), state.LastConnectedAt);
            });
    }
}