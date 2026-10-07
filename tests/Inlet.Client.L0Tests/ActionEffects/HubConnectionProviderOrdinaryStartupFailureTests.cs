using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Verifies ordinary provider startup failures without network I/O.
/// </summary>
public sealed class HubConnectionProviderOrdinaryStartupFailureTests
{
    private static readonly string[] FailureFieldNames = ["ElapsedMilliseconds", "{OriginalFormat}"];

    /// <summary>
    ///     Synchronous and asynchronous startup failures preserve their identity, status and logging.
    /// </summary>
    /// <param name="enabled">Whether logging is enabled.</param>
    /// <param name="synchronous">Whether startup throws before returning a task.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task OrdinaryStartupFailurePreservesExceptionAndErrorLogging(
        bool enabled,
        bool synchronous
    )
    {
        InvalidOperationException failure = new("ordinary startup failed");
        Mock<ILogger<HubConnectionProvider>> logger = new();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(enabled);
        Mock<IInletStore> store = new();
        List<IAction> actions = [];
        store.Setup(value => value.Dispatch(It.IsAny<IAction>())).Callback<IAction>(actions.Add);
        FakeTimeProvider time = new(new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));
        CancellationToken observedToken = default;
        Func<CancellationToken, Task> connectionStarter = token =>
        {
            observedToken = token;
            if (synchronous)
            {
                throw failure;
            }

            return Task.FromException(failure);
        };
        await using HubConnectionProvider provider = new(
            new FailedStartupNavigationManager(),
            new(() => store.Object),
            null,
            time,
            logger.Object,
            connectionStarter);
        Task starting = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            starting.WaitAsync(TestContext.Current.CancellationToken));
        Assert.Same(failure, observed);
        Assert.True(starting.IsFaulted);
        Assert.Equal(TestContext.Current.CancellationToken, observedToken);
        Assert.Equal(HubConnectionState.Disconnected, provider.Connection.State);
        Assert.Collection(
            actions,
            action => Assert.IsType<SignalRConnectingAction>(action),
            action =>
            {
                SignalRDisconnectedAction disconnected = Assert.IsType<SignalRDisconnectedAction>(action);
                Assert.Equal(failure.Message, disconnected.Error);
                Assert.Equal(time.GetUtcNow(), disconnected.Timestamp);
            });
        IInvocation[] logs = logger.Invocations.Where(value => value.Method.Name == "Log").ToArray();
        if (!enabled)
        {
            Assert.Empty(logs);
            return;
        }

        Assert.Equal(2, logs.Length);
        Assert.Equal(1, Assert.IsType<EventId>(logs[0].Arguments[1]).Id);
        Assert.Equal(LogLevel.Debug, Assert.IsType<LogLevel>(logs[0].Arguments[0]));
        IInvocation failedLog = logs[1];
        Assert.Equal(LogLevel.Error, Assert.IsType<LogLevel>(failedLog.Arguments[0]));
        EventId eventId = Assert.IsType<EventId>(failedLog.Arguments[1]);
        Assert.Equal(3, eventId.Id);
        Assert.Equal("ConnectionStartFailed", eventId.Name);
        Assert.Same(failure, failedLog.Arguments[3]);
        Dictionary<string, object?> fields = Assert
            .IsType<IEnumerable<KeyValuePair<string, object?>>>(failedLog.Arguments[2], false)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        Assert.Equal(FailureFieldNames, fields.Keys);
        Assert.Equal("Initial SignalR connection failed after {ElapsedMilliseconds} ms", fields["{OriginalFormat}"]);
        Assert.InRange(Assert.IsType<double>(fields["ElapsedMilliseconds"]), 0, double.MaxValue);
    }

    /// <summary>
    ///     A failed Connecting dispatch remains outside failed-start recovery.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ThrowingConnectingDispatchDoesNotInvokeStartup()
    {
        InvalidOperationException failure = new("connecting dispatch failed");
        Mock<IInletStore> store = new();
        store.Setup(value => value.Dispatch(It.Is<IAction>(action => action is SignalRConnectingAction)))
            .Throws(failure);
        bool invoked = false;
        await using HubConnectionProvider provider = new(
            new FailedStartupNavigationManager(),
            new(() => store.Object),
            null,
            null,
            null,
            _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });
        InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.EnsureConnectedAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TestContext.Current.CancellationToken));
        Assert.Same(failure, observed);
        Assert.False(invoked);
        store.Verify(
            value => value.Dispatch(It.Is<IAction>(action => action is SignalRDisconnectedAction)),
            Times.Never);
    }

    /// <summary>
    ///     A failed Disconnected dispatch retains the original startup failure and reports the secondary error.
    /// </summary>
    /// <param name="enabled">Whether logging is enabled.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ThrowingDisconnectedDispatchRetainsOriginalFailure(
        bool enabled
    )
    {
        InvalidOperationException startupFailure = new("ordinary startup failed");
        InvalidOperationException publicationFailure = new("disconnected dispatch failed");
        Mock<IInletStore> store = new();
        store.Setup(value => value.Dispatch(It.Is<IAction>(action => action is SignalRDisconnectedAction)))
            .Throws(publicationFailure);
        Mock<ILogger<HubConnectionProvider>> logger = new();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(enabled);
        Func<CancellationToken, Task> connectionStarter = _ => Task.FromException(startupFailure);
        await using HubConnectionProvider provider = new(
            new FailedStartupNavigationManager(),
            new(() => store.Object),
            null,
            null,
            logger.Object,
            connectionStarter);
        Task starting = provider.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            starting.WaitAsync(TestContext.Current.CancellationToken));
        Assert.Same(startupFailure, observed);
        Assert.True(starting.IsFaulted);
        store.Verify(
            value => value.Dispatch(It.Is<IAction>(action => action is SignalRDisconnectedAction)),
            Times.Once);
        IInvocation[] logs = logger.Invocations.Where(value => value.Method.Name == "Log").ToArray();
        if (!enabled)
        {
            Assert.Empty(logs);
            return;
        }

        Assert.Equal(3, logs.Length);
        Assert.Same(startupFailure, logs[1].Arguments[3]);
        IInvocation publicationLog = logs[2];
        Assert.Equal(LogLevel.Error, Assert.IsType<LogLevel>(publicationLog.Arguments[0]));
        EventId eventId = Assert.IsType<EventId>(publicationLog.Arguments[1]);
        Assert.Equal(4, eventId.Id);
        Assert.Equal("ConnectionStatusPublicationFailed", eventId.Name);
        Assert.Same(publicationFailure, publicationLog.Arguments[3]);
    }
}