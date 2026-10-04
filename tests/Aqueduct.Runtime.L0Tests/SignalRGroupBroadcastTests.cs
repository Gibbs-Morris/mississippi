using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Runtime.Diagnostics;
using Mississippi.Aqueduct.Runtime.Grains;
using Mississippi.Testing.Utilities.Mocks;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Orleans;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Verifies broadcast telemetry when group membership changes during delivery.
/// </summary>
public sealed class SignalRGroupBroadcastTests
{
    /// <summary>
    ///     Creates three completed client sends and a direct grain over their immutable membership.
    /// </summary>
    /// <param name="hubName">The isolated hub identity.</param>
    /// <param name="args">The expected message arguments.</param>
    /// <returns>The grain and its factory, clients and logger.</returns>
    private static async Task<(SignalRGroupGrain Group, IGrainFactory Factory,
            Dictionary<string, ISignalRClientGrain> Clients, ILogger<SignalRGroupGrain> Logger)>
        CreateBroadcastFixtureAsync(
            string hubName,
            ImmutableArray<object?> args
        )
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ILogger<SignalRGroupGrain> logger = Substitute.For<ILogger<SignalRGroupGrain>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        SignalRGroupGrain group = new(
            GrainContextMockBuilder.Create().WithGrainKey($"{hubName}:group").BuildObject(),
            factory,
            logger);
        Dictionary<string, ISignalRClientGrain> clients = new();
        foreach (string connection in new[] { "one", "two", "three" })
        {
            ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
            client.SendMessageAsync("update", args).Returns(Task.CompletedTask);
            factory.GetGrain<ISignalRClientGrain>($"{hubName}:{connection}").Returns(client);
            clients.Add(connection, client);
            await group.AddConnectionAsync(connection);
        }

        return (group, factory, clients, logger);
    }

    /// <summary>
    ///     A failed member must not prevent attempts to the remaining snapshot members or report success.
    /// </summary>
    /// <param name="failResolution">Whether resolving the failed client throws.</param>
    /// <param name="throwSynchronously">Whether the failed send throws before returning a task.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task BroadcastShouldAttemptEverySnapshotMemberAfterFailure(
        bool failResolution,
        bool throwSynchronously
    )
    {
        string hubName =
            $"{nameof(BroadcastShouldAttemptEverySnapshotMemberAfterFailure)}-{failResolution}-{throwSynchronously}";
        ImmutableArray<object?> args = ["payload", 42];
        (SignalRGroupGrain group, IGrainFactory factory, Dictionary<string, ISignalRClientGrain> clients,
            ILogger<SignalRGroupGrain> logger) = await CreateBroadcastFixtureAsync(hubName, args);
        ImmutableHashSet<string> snapshot = await group.GetConnectionsAsync();
        string failedConnection = snapshot.First();
        InvalidOperationException expectedFailure = new("recipient unavailable");
        if (failResolution)
        {
            factory.GetGrain<ISignalRClientGrain>($"{hubName}:{failedConnection}").Throws(expectedFailure);
        }
        else if (throwSynchronously)
        {
            clients[failedConnection].SendMessageAsync("update", args).Throws(expectedFailure);
        }
        else
        {
            clients[failedConnection].SendMessageAsync("update", args).Returns(Task.FromException(expectedFailure));
        }

        Exception? failure = await Record.ExceptionAsync(() => group.SendMessageAsync("update", args));
        Assert.Same(expectedFailure, failure);
        foreach (string connection in snapshot.Where(connection => connection != failedConnection))
        {
            await clients[connection].Received(1).SendMessageAsync("update", args);
        }

        Assert.DoesNotContain(
            logger.ReceivedCalls(),
            call => (call.GetMethodInfo().Name == "Log") && (Assert.IsType<EventId>(call.GetArguments()[1]).Id == 10));
    }

    /// <summary>
    ///     Reporting a failure must wait for the healthy member's pending send to complete.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task BroadcastShouldAwaitHealthyMembersBeforeReportingFailure()
    {
        string hubName = nameof(BroadcastShouldAwaitHealthyMembersBeforeReportingFailure);
        ImmutableArray<object?> args = ["payload"];
        (SignalRGroupGrain group, IGrainFactory _, Dictionary<string, ISignalRClientGrain> clients,
            ILogger<SignalRGroupGrain> _) = await CreateBroadcastFixtureAsync(hubName, args);
        ImmutableHashSet<string> snapshot = await group.GetConnectionsAsync();
        string failedConnection = snapshot.First();
        string pendingConnection = snapshot.First(connection => connection != failedConnection);
        InvalidOperationException expectedFailure = new("recipient unavailable");
        clients[failedConnection].SendMessageAsync("update", args).Returns(Task.FromException(expectedFailure));
        TaskCompletionSource pendingSend = new(TaskCreationOptions.RunContinuationsAsynchronously);
        clients[pendingConnection].SendMessageAsync("update", args).Returns(pendingSend.Task);
        Task<Exception?> broadcast = Record.ExceptionAsync(() => group.SendMessageAsync("update", args)).AsTask();
        bool completedBeforePendingSend = broadcast.IsCompleted;
        pendingSend.SetResult();
        Exception? failure = await broadcast;
        Assert.False(completedBeforePendingSend);
        Assert.Same(expectedFailure, failure);
        await clients[pendingConnection].Received(1).SendMessageAsync("update", args);
    }

    /// <summary>
    ///     Delivery, metrics, and completion logs use the membership snapshot taken before awaiting clients.
    /// </summary>
    /// <param name="remove">Whether to remove the original member rather than add a new member.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BroadcastShouldReportItsOriginalFanout(
        bool remove
    )
    {
        string hubName = $"{nameof(SignalRGroupBroadcastTests)}-{remove}";
        ConcurrentQueue<int> fanouts = new();
        using MeterListener listener = new();
        listener.InstrumentPublished = (instrument, activeListener) =>
        {
            if ((instrument.Meter.Name == AqueductMetrics.MeterName) &&
                (instrument.Name == "signalr.group.fanout.size"))
            {
                activeListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if ((tag.Key == "hub.name") && Equals(tag.Value, hubName))
                {
                    fanouts.Enqueue(value);
                }
            }
        });
        listener.Start();
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
        TaskCompletionSource delivery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.GetGrain<ISignalRClientGrain>($"{hubName}:original").Returns(client);
        client.SendMessageAsync("update", Arg.Any<ImmutableArray<object?>>()).Returns(delivery.Task);
        ILogger<SignalRGroupGrain> logger = Substitute.For<ILogger<SignalRGroupGrain>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        SignalRGroupGrain group = new(
            GrainContextMockBuilder.Create().WithGrainKey($"{hubName}:group").BuildObject(),
            factory,
            logger);
        await group.AddConnectionAsync("original");
        Task broadcast = group.SendMessageAsync("update", []);
        Assert.False(broadcast.IsCompleted);
        if (remove)
        {
            await group.RemoveConnectionAsync("original");
        }
        else
        {
            await group.AddConnectionAsync("late");
        }

        delivery.SetResult();
        await broadcast;
        Assert.Equal(1, Assert.Single(fanouts));
        object?[] completionLog = logger.ReceivedCalls()
            .Single(call => (call.GetMethodInfo().Name == "Log") &&
                            (Assert.IsType<EventId>(call.GetArguments()[1]).Id == 10))
            .GetArguments();
        IEnumerable<KeyValuePair<string, object?>> fields =
            Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(completionLog[2], false);
        Assert.Equal(1, Assert.IsType<int>(fields.Single(field => field.Key == "ConnectionCount").Value));
        _ = factory.DidNotReceive().GetGrain<ISignalRClientGrain>($"{hubName}:late");
    }
}