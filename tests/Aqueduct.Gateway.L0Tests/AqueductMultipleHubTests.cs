using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using Mississippi.Aqueduct.Abstractions.Messages;

using NSubstitute;

using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Verifies routing isolation through the public registration path for distinct hubs.
/// </summary>
public sealed class AqueductMultipleHubTests
{
    /// <summary>
    ///     A stream broadcast reaches only the clients of the hub that subscribed to it.
    /// </summary>
    /// <param name="reverse">Whether the second hub initializes first.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BroadcastCallbackShouldOnlyReachItsHub(
        bool reverse
    )
    {
        using AqueductMultipleHubFixture fixture = new();
        await fixture.ConnectBothAsync(reverse);
        string firstHub = reverse ? nameof(SecondAqueductHub) : nameof(TestAqueductHub);
        HubConnectionContext expected = reverse ? fixture.SecondConnection : fixture.FirstConnection;
        AllMessage message = new()
        {
            MethodName = "update",
            Args = ["payload"],
        };
        await fixture.AllObservers[AqueductMultipleHubFixture.BroadcastId(firstHub)].OnNextAsync(message);
        await fixture.Sender.Received(1).SendAsync(expected, "update", message.Args);
        await fixture.Sender.Received(1)
            .SendAsync(Arg.Any<HubConnectionContext>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<object?>>());
    }

    /// <summary>
    ///     The later initialized hub must publish through its own broadcast stream.
    /// </summary>
    /// <param name="reverse">Whether the second hub initializes first.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BroadcastPublicationShouldUseCallingHubsChannel(
        bool reverse
    )
    {
        using AqueductMultipleHubFixture fixture = new();
        await fixture.ConnectBothAsync(reverse);
        if (reverse)
        {
            await fixture.FirstManager.SendAllAsync("update", ["payload"], TestContext.Current.CancellationToken);
        }
        else
        {
            await fixture.SecondManager.SendAllAsync("update", ["payload"], TestContext.Current.CancellationToken);
        }

        StreamId expected = AqueductMultipleHubFixture.BroadcastId(
            reverse ? nameof(TestAqueductHub) : nameof(SecondAqueductHub));
        Assert.Equal(expected, Assert.Single(fixture.PublishedStreams));
    }

    /// <summary>
    ///     Disconnecting one hub leaves server delivery to the other hub intact and singular.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task DisconnectShouldPreserveOtherHubServerDelivery()
    {
        using AqueductMultipleHubFixture fixture = new();
        await fixture.ConnectBothAsync();
        await fixture.FirstManager.OnDisconnectedAsync(fixture.FirstConnection);
        ServerMessage message = new()
        {
            ConnectionId = fixture.SecondConnection.ConnectionId,
            MethodName = "update",
            Args = ["payload"],
        };
        foreach (IAsyncObserver<ServerMessage> observer in fixture.ServerObservers)
        {
            await observer.OnNextAsync(message);
        }

        await fixture.Sender.Received(1).SendAsync(fixture.SecondConnection, "update", message.Args);
        await fixture.Sender.Received(1)
            .SendAsync(Arg.Any<HubConnectionContext>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<object?>>());
        await fixture.Heartbeat.DidNotReceive().StopAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Both public registration overloads must subscribe each hub regardless of connection order.
    /// </summary>
    /// <param name="configureOptions">Whether registrations include an options callback.</param>
    /// <param name="reverse">Whether the second hub initializes first.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task EachRegisteredHubShouldSubscribeItsBroadcastChannel(
        bool configureOptions,
        bool reverse
    )
    {
        using AqueductMultipleHubFixture fixture = new(configureOptions);
        await fixture.ConnectBothAsync(reverse);
        Assert.Contains(AqueductMultipleHubFixture.BroadcastId(nameof(TestAqueductHub)), fixture.AllObservers.Keys);
        Assert.Contains(AqueductMultipleHubFixture.BroadcastId(nameof(SecondAqueductHub)), fixture.AllObservers.Keys);
    }

    /// <summary>
    ///     The shared heartbeat counts both hubs and retains the remaining hub after disconnect.
    /// </summary>
    /// <param name="reverse">Whether the second hub initializes first.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeartbeatShouldCountConnectionsAcrossHubs(
        bool reverse
    )
    {
        using AqueductMultipleHubFixture fixture = new();
        await fixture.ConnectBothAsync(reverse);
        Func<int> count = Assert.IsType<Func<int>>(fixture.FirstHeartbeatCountProvider);
        Assert.Equal(2, count());
        await fixture.FirstManager.OnDisconnectedAsync(fixture.FirstConnection);
        Assert.Equal(1, count());
        await fixture.SecondManager.OnDisconnectedAsync(fixture.SecondConnection);
        Assert.Equal(0, count());
        await fixture.Heartbeat.DidNotReceive().StopAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Disposing one lifetime manager does not stop the other hub's live routing.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task ManagerDisposalShouldPreserveOtherHubRouting()
    {
        using AqueductMultipleHubFixture fixture = new();
        await fixture.ConnectBothAsync();
        using IDisposable first = Assert.IsType<IDisposable>(fixture.FirstManager, false);
        first.Dispose();
        AllMessage message = new()
        {
            MethodName = "update",
            Args = ["payload"],
        };
        await fixture.SecondManager.SendAllAsync("update", [.. message.Args], TestContext.Current.CancellationToken);
        await fixture.AllObservers[AqueductMultipleHubFixture.BroadcastId(nameof(SecondAqueductHub))]
            .OnNextAsync(message);
        await fixture.Sender.Received(1).SendAsync(fixture.SecondConnection, "update", message.Args);
        await fixture.Heartbeat.DidNotReceive().StopAsync(Arg.Any<CancellationToken>());
        fixture.Heartbeat.DidNotReceive().Dispose();
    }

    /// <summary>
    ///     A single connected hub retains local targeted delivery and its broadcast callback.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task SingleHubShouldRetainLocalAndBroadcastDelivery()
    {
        using AqueductMultipleHubFixture fixture = new();
        await fixture.FirstManager.OnConnectedAsync(fixture.FirstConnection);
        object?[] args = ["payload"];
        await fixture.FirstManager.SendConnectionAsync(
            fixture.FirstConnection.ConnectionId,
            "local",
            args,
            TestContext.Current.CancellationToken);
        await fixture.AllObservers[AqueductMultipleHubFixture.BroadcastId(nameof(TestAqueductHub))]
            .OnNextAsync(
                new()
                {
                    MethodName = "broadcast",
                    Args = args,
                });
        await fixture.Sender.Received(1).SendAsync(fixture.FirstConnection, "local", args);
        await fixture.Sender.Received(1).SendAsync(fixture.FirstConnection, "broadcast", args);
        Assert.Single(fixture.AllObservers);
    }

    /// <summary>
    ///     A connection belonging to another hub is routed through the caller's grain, never its local transport.
    /// </summary>
    /// <param name="reverse">Whether the second hub initializes first.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TargetedSendShouldNotUseAnotherHubsLocalConnection(
        bool reverse
    )
    {
        using AqueductMultipleHubFixture fixture = new();
        await fixture.ConnectBothAsync(reverse);
        fixture.Grains.ClearReceivedCalls();
        await fixture.FirstManager.SendConnectionAsync(
            fixture.SecondConnection.ConnectionId,
            "update",
            ["payload"],
            TestContext.Current.CancellationToken);
        _ = fixture.Grains.Received(1).GetClientGrain(nameof(TestAqueductHub), fixture.SecondConnection.ConnectionId);
        await fixture.Client.Received(1)
            .SendMessageAsync(
                "update",
                Arg.Is<ImmutableArray<object?>>(args => (args.Length == 1) && Equals(args[0], "payload")));
        await fixture.Sender.DidNotReceive()
            .SendAsync(Arg.Any<HubConnectionContext>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<object?>>());
    }
}