using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Verifies that a failed caller does not disconnect a competing transport startup.
/// </summary>
public sealed class HubConnectionProviderCompetingStartupTests
{
    /// <summary>
    ///     A canceled or rejected caller leaves an active or connected competing transport's status unchanged.
    /// </summary>
    /// <param name="cancelWhileConnecting">Whether to cancel while the competing start is pending.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompetingStartupDoesNotPublishFalseDisconnected(
        bool cancelWhileConnecting
    )
    {
        TimeSpan timeout = TimeSpan.FromSeconds(15);
        await using StartupStatusServer server = new();
        await server.StartAsync(TestContext.Current.CancellationToken);
        ConcurrentQueue<IAction> actions = new();
        Mock<IInletStore> store = new();
        await using HubConnectionProvider provider = new(
            new StartupStatusNavigationManager(server.BaseUri),
            new(() => store.Object));
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
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task starting = provider.EnsureConnectedAsync(cancellation.Token);
        await server.FirstNegotiation.WaitAsync(timeout, TestContext.Current.CancellationToken);
        Assert.Equal(HubConnectionState.Connecting, provider.Connection.State);
        if (cancelWhileConnecting)
        {
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => starting.WaitAsync(
                timeout,
                TestContext.Current.CancellationToken));
            Assert.True(starting.IsCanceled);
            Assert.Equal(HubConnectionState.Connecting, provider.Connection.State);
        }

        server.CompleteFirstNegotiation(200);
        await competingStart.WaitAsync(timeout, TestContext.Current.CancellationToken);
        if (!cancelWhileConnecting)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => starting.WaitAsync(
                timeout,
                TestContext.Current.CancellationToken));
        }

        Assert.Equal(HubConnectionState.Connected, provider.Connection.State);
        Assert.True(provider.IsConnected);
        Assert.IsType<SignalRConnectingAction>(Assert.Single(actions));
    }
}