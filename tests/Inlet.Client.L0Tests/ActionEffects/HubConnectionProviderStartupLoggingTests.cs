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
///     Verifies cancellation and the provider's structured logging contracts.
/// </summary>
public sealed class HubConnectionProviderStartupLoggingTests
{
    private static readonly string[] CompletionFieldNames =
        ["ConnectionState", "ElapsedMilliseconds", "{OriginalFormat}"];

    private static readonly string[] FailureFieldNames = ["ElapsedMilliseconds", "{OriginalFormat}"];

    private static Dictionary<string, object?> AssertLog(
        IInvocation log,
        LogLevel level,
        int eventId,
        string eventName,
        string template,
        Exception? exception = null
    )
    {
        Assert.Equal(level, Assert.IsType<LogLevel>(log.Arguments[0]));
        EventId actualEvent = Assert.IsType<EventId>(log.Arguments[1]);
        Assert.Equal(eventId, actualEvent.Id);
        Assert.Equal(eventName, actualEvent.Name);
        if (exception is null)
        {
            Assert.Null(log.Arguments[3]);
        }
        else
        {
            Assert.Same(exception, log.Arguments[3]);
        }

        Dictionary<string, object?> fields = Assert
            .IsType<IEnumerable<KeyValuePair<string, object?>>>(log.Arguments[2], false)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        Assert.Equal(template, fields["{OriginalFormat}"]);
        return fields;
    }

    private static Mock<ILogger<HubConnectionProvider>> CreateLogger(
        bool enabled
    )
    {
        Mock<ILogger<HubConnectionProvider>> logger = new();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(enabled);
        return logger;
    }

    /// <summary>
    ///     An already canceled caller retains cancellation and publishes a terminal state without network I/O.
    /// </summary>
    /// <param name="enabled">Whether logging is enabled.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AlreadyCanceledStartupPreservesFailureAndLogging(
        bool enabled
    )
    {
        Mock<ILogger<HubConnectionProvider>> logger = CreateLogger(enabled);
        Mock<IInletStore> store = new();
        List<IAction> actions = [];
        store.Setup(value => value.Dispatch(It.IsAny<IAction>())).Callback<IAction>(actions.Add);
        FakeTimeProvider time = new(new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));
        await using HubConnectionProvider provider = new(
            new FailedStartupNavigationManager(),
            new(() => store.Object),
            timeProvider: time,
            logger: logger.Object);
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        Task starting = provider.EnsureConnectedAsync(cancellation.Token);
        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                starting.WaitAsync(TestContext.Current.CancellationToken));
        Assert.True(starting.IsCanceled);
        Assert.Equal(HubConnectionState.Disconnected, provider.Connection.State);
        Assert.Collection(
            actions,
            action => Assert.IsType<SignalRConnectingAction>(action),
            action =>
            {
                SignalRDisconnectedAction disconnected = Assert.IsType<SignalRDisconnectedAction>(action);
                Assert.Equal(exception.Message, disconnected.Error);
                Assert.Equal(time.GetUtcNow(), disconnected.Timestamp);
            });
        IInvocation[] logs = logger.Invocations.Where(log => log.Method.Name == "Log").ToArray();
        if (!enabled)
        {
            Assert.Empty(logs);
            return;
        }

        Assert.Equal(2, logs.Length);
        Dictionary<string, object?> entry = AssertLog(
            logs[0],
            LogLevel.Debug,
            1,
            "EnsureConnectionStarted",
            "Ensuring SignalR connection in {ConnectionState}");
        Assert.Equal(HubConnectionState.Disconnected, entry["ConnectionState"]);
        Dictionary<string, object?> failure = AssertLog(
            logs[1],
            LogLevel.Information,
            3,
            "ConnectionStartFailed",
            "Initial SignalR connection failed after {ElapsedMilliseconds} ms",
            exception);
        Assert.InRange(Assert.IsType<double>(failure["ElapsedMilliseconds"]), 0, double.MaxValue);
    }

    /// <summary>
    ///     Completion logging retains its state, elapsed time and event identity.
    /// </summary>
    /// <param name="enabled">Whether logging is enabled.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompletionLoggingPreservesFields(
        bool enabled
    )
    {
        Mock<ILogger<HubConnectionProvider>> logger = CreateLogger(enabled);
        logger.Object.EnsureConnectionCompleted(HubConnectionState.Connected, 12.5);
        IInvocation[] logs = logger.Invocations.Where(log => log.Method.Name == "Log").ToArray();
        if (!enabled)
        {
            Assert.Empty(logs);
            return;
        }

        IInvocation log = Assert.Single(logs);
        Dictionary<string, object?> fields = AssertLog(
            log,
            LogLevel.Debug,
            2,
            "EnsureConnectionCompleted",
            "SignalR connection check completed in {ConnectionState} after {ElapsedMilliseconds} ms");
        Assert.Equal(HubConnectionState.Connected, fields["ConnectionState"]);
        Assert.Equal(12.5, fields["ElapsedMilliseconds"]);
        Assert.Equal(CompletionFieldNames, fields.Keys);
        object? state = log.Arguments[2];
        Assert.NotNull(state);
        Assert.Equal("SignalR connection check completed in Connected after 12.5 ms", state.ToString());
    }

    /// <summary>
    ///     Failure logging retains its exception, elapsed time and error severity.
    /// </summary>
    /// <param name="enabled">Whether logging is enabled.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailureLoggingPreservesException(
        bool enabled
    )
    {
        Mock<ILogger<HubConnectionProvider>> logger = CreateLogger(enabled);
        InvalidOperationException exception = new("startup failed");
        logger.Object.ConnectionStartFailed(LogLevel.Error, exception, 12.5);
        IInvocation[] logs = logger.Invocations.Where(log => log.Method.Name == "Log").ToArray();
        if (!enabled)
        {
            Assert.Empty(logs);
            return;
        }

        IInvocation log = Assert.Single(logs);
        Dictionary<string, object?> fields = AssertLog(
            log,
            LogLevel.Error,
            3,
            "ConnectionStartFailed",
            "Initial SignalR connection failed after {ElapsedMilliseconds} ms",
            exception);
        Assert.Equal(12.5, fields["ElapsedMilliseconds"]);
        Assert.Equal(FailureFieldNames, fields.Keys);
        object? state = log.Arguments[2];
        Assert.NotNull(state);
        Assert.Equal("Initial SignalR connection failed after 12.5 ms", state.ToString());
    }
}