using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Verifies failure diagnostics when a competing transport owns startup.
/// </summary>
public sealed class HubConnectionProviderCompetingStartupDiagnosticsTests
{
    /// <summary>
    ///     A competing transport cannot suppress the original startup failure log.
    /// </summary>
    /// <param name="cancelWhileConnecting">Whether the caller cancels before the competing start completes.</param>
    /// <returns>A task representing the regression.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompetingStartupRetainsOriginalFailureLog(
        bool cancelWhileConnecting
    )
    {
        TimeSpan timeout = TimeSpan.FromSeconds(15);
        await using StartupStatusServer server = new();
        await server.StartAsync(TestContext.Current.CancellationToken);
        Mock<IInletStore> store = new();
        Mock<ILogger<HubConnectionProvider>> logger = new();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        await using HubConnectionProvider provider = new(
            new StartupStatusNavigationManager(server.BaseUri),
            new(() => store.Object),
            logger: logger.Object);
        Task competingStart = Task.CompletedTask;
        store.Setup(value => value.Dispatch(It.IsAny<IAction>()))
            .Callback<IAction>(action =>
            {
                if (action is SignalRConnectingAction)
                {
                    competingStart = provider.Connection.StartAsync(TestContext.Current.CancellationToken);
                }
            });
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task starting = provider.EnsureConnectedAsync(cancellation.Token);
        await server.FirstNegotiation.WaitAsync(timeout, TestContext.Current.CancellationToken);
        Exception original;
        if (cancelWhileConnecting)
        {
            await cancellation.CancelAsync();
            original = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => starting.WaitAsync(
                timeout,
                TestContext.Current.CancellationToken));
            Assert.True(starting.IsCanceled);
            Assert.Equal(HubConnectionState.Connecting, provider.Connection.State);
        }
        else
        {
            server.CompleteFirstNegotiation(200);
            await competingStart.WaitAsync(timeout, TestContext.Current.CancellationToken);
            original = await Assert.ThrowsAsync<InvalidOperationException>(() => starting.WaitAsync(
                timeout,
                TestContext.Current.CancellationToken));
            Assert.True(starting.IsFaulted);
            Assert.Equal(HubConnectionState.Connected, provider.Connection.State);
        }

        IInvocation failureLog = Assert.Single(
            logger.Invocations,
            value => (value.Method.Name == "Log") && (Assert.IsType<EventId>(value.Arguments[1]).Id == 3));
        Assert.Same(original, failureLog.Arguments[3]);
        Assert.Equal(
            cancelWhileConnecting ? LogLevel.Information : LogLevel.Error,
            Assert.IsType<LogLevel>(failureLog.Arguments[0]));
        server.CompleteFirstNegotiation(200);
        await competingStart.WaitAsync(timeout, TestContext.Current.CancellationToken);
    }
}