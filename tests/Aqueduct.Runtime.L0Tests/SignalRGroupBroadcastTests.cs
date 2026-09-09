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

using Orleans;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Verifies broadcast telemetry when group membership changes during delivery.
/// </summary>
public sealed class SignalRGroupBroadcastTests
{
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