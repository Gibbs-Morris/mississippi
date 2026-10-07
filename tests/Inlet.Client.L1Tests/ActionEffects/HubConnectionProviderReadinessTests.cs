using System;
using System.Net.Http;
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
///     Exercises production readiness against a real local SignalR connection.
/// </summary>
public sealed class HubConnectionProviderReadinessTests
{
    private static async Task ConnectAsync(
        HubConnectionProvider provider,
        ControlledSignalRServer server,
        CancellationToken cancellationToken
    )
    {
        Task connecting = provider.EnsureConnectedAsync(cancellationToken);
        PendingNegotiation negotiation = await server.NextNegotiationAsync(cancellationToken);
        negotiation.Allow();
        await connecting;
    }

    private static HubConnectionProvider CreateProvider(
        ControlledSignalRServer server,
        Mock<IInletStore>? store = null
    ) =>
        new(
            new ReadinessNavigationManager(server.Address),
            new(() => (store ?? new Mock<IInletStore>()).Object),
            new()
            {
                HubPath = "/hub",
            },
            new FakeTimeProvider());

    private static async Task<string> InvokeWhenReadyAsync(
        HubConnectionProvider provider,
        CancellationToken cancellationToken
    )
    {
        await provider.EnsureConnectedAsync(cancellationToken);
        return await provider.Connection.InvokeAsync<string>("Echo", "reconnected", cancellationToken);
    }

    /// <summary>
    ///     Reconnect readiness can be canceled without stopping the connection's retry.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task CancelledWaitLeavesAutomaticReconnectRunning()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        using CancellationTokenSource waiterCancellation = new();
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        await using HubConnectionProvider provider = CreateProvider(server);
        await ConnectAsync(provider, server, timeout.Token);
        HubCallerContext connection = await server.NextConnectionAsync(timeout.Token);
        connection.GetHttpContext()!.Abort();
        PendingNegotiation negotiation = await server.NextNegotiationAsync(timeout.Token);
        Task readiness = provider.EnsureConnectedAsync(waiterCancellation.Token);
        try
        {
            await waiterCancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readiness.WaitAsync(timeout.Token));
            Assert.Equal(HubConnectionState.Reconnecting, provider.Connection.State);
        }
        finally
        {
            negotiation.Allow();
        }

        await provider.EnsureConnectedAsync(timeout.Token);
        Assert.True(provider.IsConnected);
    }

    /// <summary>
    ///     Canceling a waiting caller leaves the initial start available to its owner.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task CancelledWaitLeavesInitialStartRunning()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        using CancellationTokenSource waiterCancellation = new();
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        await using HubConnectionProvider provider = CreateProvider(server);
        Task first = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation negotiation = await server.NextNegotiationAsync(timeout.Token);
        Task second = provider.EnsureConnectedAsync(waiterCancellation.Token);
        try
        {
            await waiterCancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.WaitAsync(timeout.Token));
            Assert.False(first.IsCompleted);
        }
        finally
        {
            negotiation.Allow();
            await first;
        }

        Assert.True(provider.IsConnected);
    }

    /// <summary>
    ///     An overlapping readiness call remains pending until the first start is usable.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task ConcurrentEnsureWaitsForInitialStart()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        Mock<IInletStore> store = new();
        await using HubConnectionProvider provider = CreateProvider(server, store);
        Task first = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation negotiation = await server.NextNegotiationAsync(timeout.Token);
        Assert.Equal(HubConnectionState.Connecting, provider.Connection.State);
        Task second = provider.EnsureConnectedAsync(timeout.Token);
        try
        {
            Assert.False(second.IsCompleted);
        }
        finally
        {
            negotiation.Allow();
            await first;
        }

        await second;
        Assert.True(provider.IsConnected);
        Assert.Equal("ready", await provider.Connection.InvokeAsync<string>("Echo", "ready", timeout.Token));
        Assert.Equal(1, server.NegotiationCount);
        store.Verify(value => value.Dispatch(It.IsAny<SignalRConnectingAction>()), Times.Once);
        store.Verify(value => value.Dispatch(It.IsAny<SignalRConnectedAction>()), Times.Once);
    }

    /// <summary>
    ///     A ready connection accepts repeated readiness calls without starting again.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task ConnectedEnsureReusesUsableConnection()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        await using HubConnectionProvider provider = CreateProvider(server);
        Task connecting = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation negotiation = await server.NextNegotiationAsync(timeout.Token);
        Task<string> invocation = provider.Connection.InvokeAsync<string>("Echo", "control", timeout.Token);
        try
        {
            Assert.False(invocation.IsCompleted);
        }
        finally
        {
            negotiation.Allow();
        }

        await connecting;
        Assert.Equal("control", await invocation);
        await provider.EnsureConnectedAsync(timeout.Token);
        Assert.Equal("control", await provider.Connection.InvokeAsync<string>("Echo", "control", timeout.Token));
        Assert.Equal(1, server.NegotiationCount);
    }

    /// <summary>
    ///     Readiness holds a hub invocation through a failed reconnect attempt and its retry.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task EnsureWaitsForAutomaticReconnect()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        await using HubConnectionProvider provider = CreateProvider(server);
        await ConnectAsync(provider, server, timeout.Token);
        HubCallerContext connection = await server.NextConnectionAsync(timeout.Token);
        connection.GetHttpContext()!.Abort();
        PendingNegotiation negotiation = await server.NextNegotiationAsync(timeout.Token);
        Assert.Equal(HubConnectionState.Reconnecting, provider.Connection.State);
        Task<string> invocation = InvokeWhenReadyAsync(provider, timeout.Token);
        negotiation.Reject();
        PendingNegotiation retry = await server.NextNegotiationAsync(timeout.Token);
        try
        {
            Assert.False(invocation.IsCompleted);
        }
        finally
        {
            retry.Allow();
        }

        Assert.Equal("reconnected", await invocation);
        Assert.True(provider.IsConnected);
        Assert.Equal(
            "reconnected",
            await provider.Connection.InvokeAsync<string>("Echo", "reconnected", timeout.Token));
        Assert.Equal(3, server.NegotiationCount);
    }

    /// <summary>
    ///     Waiting callers observe startup failure instead of a premature success.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task FailedStartIsObservedByWaitingCaller()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        await using HubConnectionProvider provider = CreateProvider(server);
        Task first = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation negotiation = await server.NextNegotiationAsync(timeout.Token);
        Task second = provider.EnsureConnectedAsync(timeout.Token);
        negotiation.Reject();
        await Assert.ThrowsAsync<HttpRequestException>(() => first.WaitAsync(timeout.Token));
        await Assert.ThrowsAsync<HttpRequestException>(() => second.WaitAsync(timeout.Token));
        Assert.False(provider.IsConnected);
        Assert.Equal(1, server.NegotiationCount);
        await ConnectAsync(provider, server, timeout.Token);
        Assert.True(provider.IsConnected);
    }

    /// <summary>
    ///     Stopping reconnect releases its readiness waiter with a failure.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task StoppedReconnectFailsReadinessWaiter()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        await using HubConnectionProvider provider = CreateProvider(server);
        await ConnectAsync(provider, server, timeout.Token);
        HubCallerContext connection = await server.NextConnectionAsync(timeout.Token);
        connection.GetHttpContext()!.Abort();
        PendingNegotiation negotiation = await server.NextNegotiationAsync(timeout.Token);
        Task readiness = provider.EnsureConnectedAsync(timeout.Token);
        Task stopping = provider.Connection.StopAsync(timeout.Token);
        negotiation.Allow();
        await stopping;
        OperationCanceledException error =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readiness.WaitAsync(timeout.Token));
        Assert.NotEqual(timeout.Token, error.CancellationToken);
        Assert.False(provider.IsConnected);
    }
}