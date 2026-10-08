using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Verifies the ordering of provider-managed startup and failed-status publication.
/// </summary>
public sealed class HubConnectionProviderFailurePublicationRaceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     A competing ensure call waits until the existing transport startup is usable.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ActiveStartupWaitsForReadiness()
    {
        await using StartupStatusServer server = new();
        await server.StartAsync(TestContext.Current.CancellationToken);
        Mock<IInletStore> store = new();
        await using HubConnectionProvider provider = new(
            new StartupStatusNavigationManager(server.BaseUri),
            new(() => store.Object));
        Task starting = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await server.FirstNegotiation.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Task joined = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        Assert.False(joined.IsCompleted);
        Assert.False(starting.IsCompleted);
        Assert.Equal(HubConnectionState.Connecting, provider.Connection.State);
        server.CompleteFirstNegotiation(200);
        await starting.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await joined.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Assert.Equal(HubConnectionState.Connected, provider.Connection.State);
    }

    /// <summary>
    ///     A retry triggered after the failure state check cannot precede its Disconnected action.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task RetryFromFailureLogDoesNotPublishStaleDisconnected()
    {
        await using StartupStatusServer server = new();
        await server.StartAsync(TestContext.Current.CancellationToken);
        Mock<IInletStore> store = new();
        ConcurrentQueue<HubConnectionState> disconnectedStates = new();
        Task retry = Task.CompletedTask;
        HubConnectionProvider? current = null;
        StartupFailureRetryLogger logger = new(() =>
            retry = current!.EnsureConnectedAsync(TestContext.Current.CancellationToken));
        await using HubConnectionProvider provider = new(
            new StartupStatusNavigationManager(server.BaseUri),
            new(() => store.Object),
            logger: logger);
        current = provider;
        store.Setup(value => value.Dispatch(It.IsAny<IAction>()))
            .Callback<IAction>(action =>
            {
                if (action is SignalRDisconnectedAction)
                {
                    disconnectedStates.Enqueue(provider.Connection.State);
                }
            });
        Task starting = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await server.FirstNegotiation.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        server.CompleteFirstNegotiation(401);
        HttpRequestException failure = await Assert.ThrowsAsync<HttpRequestException>(() =>
            starting.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        await retry.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Assert.Equal(2, server.NegotiationCount);
        Assert.Equal(HubConnectionState.Connected, provider.Connection.State);
        Assert.Equal(HubConnectionState.Disconnected, Assert.Single(disconnectedStates));
    }
}