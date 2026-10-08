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
///     Covers readiness lifetime and reentrant connection notifications.
/// </summary>
public sealed class HubConnectionProviderLifecycleTests
{
    private static HubConnectionProvider CreateProvider(
        ControlledSignalRServer server,
        Mock<IInletStore> store
    ) =>
        new(
            new ReadinessNavigationManager(server.Address),
            new(() => store.Object),
            new()
            {
                HubPath = "/hub",
            },
            new FakeTimeProvider());

    /// <summary>
    ///     Asynchronous connecting notifications join the published initial start.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task AsynchronousReentrantEnsureJoinsInitialStart()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        Mock<IInletStore> store = new();
        await using HubConnectionProvider provider = CreateProvider(server, store);
        TaskCompletionSource<Task> reentrantStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? notification = null;
        store.Setup(value => value.Dispatch(It.IsAny<SignalRConnectingAction>()))
            .Callback(() => notification = Task.Run(
                async () =>
                {
                    Task readiness = provider.EnsureConnectedAsync(timeout.Token);
                    reentrantStarted.SetResult(readiness);
                    await readiness;
                },
                timeout.Token));
        Task starting = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation negotiation = await server.NextNegotiationAsync(timeout.Token);
        Task reentrant = await reentrantStarted.Task.WaitAsync(timeout.Token);
        try
        {
            Assert.False(reentrant.IsCompleted);
            Assert.Equal(1, server.NegotiationCount);
        }
        finally
        {
            negotiation.Allow();
        }

        await starting.WaitAsync(timeout.Token);
        Assert.NotNull(notification);
        await notification.WaitAsync(timeout.Token);
        Assert.Equal("ready", await provider.Connection.InvokeAsync<string>("Echo", "ready", timeout.Token));
        store.Verify(value => value.Dispatch(It.IsAny<SignalRConnectingAction>()), Times.Once);
        store.Verify(value => value.Dispatch(It.IsAny<SignalRConnectedAction>()), Times.Once);
    }

    /// <summary>
    ///     Disposal terminates an existing wait and rejects readiness reentered from its cancellation.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task DisposalRejectsReadinessReenteredFromCancelledWait()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        await using HubConnectionProvider provider = CreateProvider(server, new());
        Task starting = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation initial = await server.NextNegotiationAsync(timeout.Token);
        initial.Allow();
        await starting.WaitAsync(timeout.Token);
        HubCallerContext connection = await server.NextConnectionAsync(timeout.Token);
        connection.GetHttpContext()!.Abort();
        PendingNegotiation retry = await server.NextNegotiationAsync(timeout.Token);
        Task? lateReadiness = null;
        HubConnectionState? lateState = null;
        ReadinessCallbackSynchronizationContext context = new(() =>
        {
            lateState = provider.Connection.State;
            lateReadiness = provider.EnsureConnectedAsync(CancellationToken.None);
        });
        SynchronizationContext? previous = SynchronizationContext.Current;
        Task readiness;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            readiness = provider.EnsureConnectedAsync(CancellationToken.None);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Task disposing = provider.DisposeAsync().AsTask();
        retry.Allow();
        await disposing.WaitAsync(timeout.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readiness.WaitAsync(timeout.Token));
        Assert.Equal(HubConnectionState.Reconnecting, lateState);
        Assert.NotNull(lateReadiness);
        Assert.True(lateReadiness.IsCompleted, "Late readiness was orphaned after provider disposal.");
        await Assert.ThrowsAsync<ObjectDisposedException>(() => lateReadiness.WaitAsync(timeout.Token));
    }

    /// <summary>
    ///     Disposing the provider cancels a reconnect readiness wait without the watchdog.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task DisposalReleasesReconnectWaiter()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        await using HubConnectionProvider provider = CreateProvider(server, new());
        Task starting = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation initial = await server.NextNegotiationAsync(timeout.Token);
        initial.Allow();
        await starting.WaitAsync(timeout.Token);
        HubCallerContext connection = await server.NextConnectionAsync(timeout.Token);
        connection.GetHttpContext()!.Abort();
        PendingNegotiation retry = await server.NextNegotiationAsync(timeout.Token);
        Task readiness = provider.EnsureConnectedAsync(timeout.Token);
        Task disposing = provider.DisposeAsync().AsTask();
        retry.Allow();
        await disposing.WaitAsync(timeout.Token);
        OperationCanceledException error =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readiness.WaitAsync(timeout.Token));
        Assert.NotEqual(timeout.Token, error.CancellationToken);
    }

    /// <summary>
    ///     A readiness call from a connecting notification joins the initial start.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task ReentrantEnsureJoinsInitialStart()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ControlledSignalRServer server = await ControlledSignalRServer.StartAsync(timeout.Token);
        Mock<IInletStore> store = new();
        await using HubConnectionProvider provider = CreateProvider(server, store);
        Task? reentrant = null;
        store.Setup(value => value.Dispatch(It.IsAny<SignalRConnectingAction>()))
            .Callback(() => reentrant = provider.EnsureConnectedAsync(timeout.Token));
        Task starting = provider.EnsureConnectedAsync(timeout.Token);
        PendingNegotiation negotiation = await server.NextNegotiationAsync(timeout.Token);
        try
        {
            Assert.NotNull(reentrant);
            Assert.False(reentrant.IsCompleted);
            Assert.Equal(1, server.NegotiationCount);
        }
        finally
        {
            negotiation.Allow();
        }

        await starting.WaitAsync(timeout.Token);
        await reentrant.WaitAsync(timeout.Token);
        Assert.Equal("ready", await provider.Connection.InvokeAsync<string>("Echo", "ready", timeout.Token));
        store.Verify(value => value.Dispatch(It.IsAny<SignalRConnectingAction>()), Times.Once);
        store.Verify(value => value.Dispatch(It.IsAny<SignalRConnectedAction>()), Times.Once);
    }
}