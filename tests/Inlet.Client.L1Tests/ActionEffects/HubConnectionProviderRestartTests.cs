using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.SignalRConnection;

using Moq;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Covers readiness across a stopped reconnect and a fresh start.
/// </summary>
public sealed class HubConnectionProviderRestartTests
{
    /// <summary>
    ///     A previous reconnect wait terminates while the fresh start remains gated.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task StoppedReconnectDoesNotFollowFreshStart()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        Mock<IInletStore> store = new();
        await using HubConnectionProvider provider = new(
            new ReadinessNavigationManager(server.Address),
            new(() => store.Object),
            new()
            {
                HubPath = "/hub",
            },
            new FakeTimeProvider());
        Task starting = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation initial = await server.NextNegotiationAsync(timeout.Token);
        initial.Allow();
        await starting.WaitAsync(timeout.Token);
        HubCallerContext connection = await server.NextConnectionAsync(timeout.Token);
        connection.GetHttpContext()!.Abort();
        PendingNegotiation retry = await server.NextNegotiationAsync(timeout.Token);
        Task reconnecting = provider.EnsureConnectedAsync(CancellationToken.None);
        Task stopping = provider.Connection.StopAsync(timeout.Token);
        retry.Allow();
        await stopping.WaitAsync(timeout.Token);
        Task restarting = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation fresh = await server.NextNegotiationAsync(timeout.Token);
        try
        {
            OperationCanceledException error =
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reconnecting.WaitAsync(timeout.Token));
            Assert.NotEqual(timeout.Token, error.CancellationToken);
            Assert.False(restarting.IsCompleted);
            Assert.Equal(HubConnectionState.Connecting, provider.Connection.State);
        }
        finally
        {
            fresh.Allow();
        }

        await restarting.WaitAsync(timeout.Token);
        Assert.Equal("ready", await provider.Connection.InvokeAsync<string>("Echo", "ready", timeout.Token));
        store.Verify(value => value.Dispatch(It.IsAny<SignalRConnectingAction>()), Times.Exactly(2));
        store.Verify(value => value.Dispatch(It.IsAny<SignalRConnectedAction>()), Times.Exactly(2));
    }
}