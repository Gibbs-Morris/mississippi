using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Messages;
using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;

using Orleans.Runtime;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Verifies that gateway readiness includes successful heartbeat startup.
/// </summary>
public sealed class AqueductHubStartupTests
{
    /// <summary>
    ///     Creates isolated server identity, stream state and directory I/O for real heartbeat startup.
    /// </summary>
    /// <returns>The server identity, grain factory, stream subscriptions and directory.</returns>
    private static (IServerIdProvider ServerId, IAqueductGrainFactory Grains, IStreamSubscriptionManager Streams,
        ISignalRServerDirectoryGrain Directory) CreateFixture()
    {
        IServerIdProvider serverId = Substitute.For<IServerIdProvider>();
        serverId.ServerId.Returns(Guid.NewGuid().ToString("N"));
        IAqueductGrainFactory grains = Substitute.For<IAqueductGrainFactory>();
        ISignalRServerDirectoryGrain directory = Substitute.For<ISignalRServerDirectoryGrain>();
        grains.GetServerDirectoryGrain().Returns(directory);
        grains.GetClientGrain(Arg.Any<string>(), Arg.Any<string>()).Returns(Substitute.For<ISignalRClientGrain>());
        IStreamSubscriptionManager streams = Substitute.For<IStreamSubscriptionManager>();
        bool initialized = false;
        streams.IsInitialized.Returns(_ => initialized);
        streams.EnsureInitializedAsync(
                Arg.Any<string>(),
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Any<Func<AllMessage, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                initialized = true;
                return Task.CompletedTask;
            });
        streams.PublishToAllAsync(Arg.Any<AllMessage>()).Returns(Task.CompletedTask);
        return (serverId, grains, streams, directory);
    }

    /// <summary>
    ///     Both broadcasts must await a directory registration that is still pending.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task ConcurrentBroadcastShouldWaitForHeartbeatStartup()
    {
        (IServerIdProvider ServerId, IAqueductGrainFactory Grains, IStreamSubscriptionManager Streams,
            ISignalRServerDirectoryGrain Directory) fixture = CreateFixture();
        using HeartbeatManager heartbeat = new(
            fixture.ServerId,
            fixture.Grains,
            Options.Create(new AqueductOptions()),
            NullLogger<HeartbeatManager>.Instance);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = new(
            fixture.ServerId,
            fixture.Grains,
            new ConnectionRegistry(),
            Substitute.For<ILocalMessageSender>(),
            heartbeat,
            fixture.Streams,
            Substitute.For<IHostApplicationLifetime>(),
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance);
        TaskCompletionSource registration = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Directory.RegisterServerAsync(heartbeat.ServerId, Arg.Any<CancellationToken>())
            .Returns(registration.Task);
        Task first = manager.SendAllAsync("update", ["first"], TestContext.Current.CancellationToken);
        Assert.True(fixture.Streams.IsInitialized);
        Task second = manager.SendAllAsync("update", ["second"], TestContext.Current.CancellationToken);
        bool firstCompletedBeforeRegistration = first.IsCompleted;
        bool secondCompletedBeforeRegistration = second.IsCompleted;
        registration.SetResult();
        await Task.WhenAll(first, second);
        Assert.False(firstCompletedBeforeRegistration);
        Assert.False(secondCompletedBeforeRegistration);
        await fixture.Directory.Received(1).RegisterServerAsync(heartbeat.ServerId, Arg.Any<CancellationToken>());
        await fixture.Streams.Received(2).PublishToAllAsync(Arg.Any<AllMessage>());
    }

    /// <summary>
    ///     Completed streams must not suppress another heartbeat registration attempt after startup fails.
    /// </summary>
    /// <param name="connect">Whether startup is invoked by a connection rather than a broadcast.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedRegistrationShouldBeRetriedAfterStreamInitialization(
        bool connect
    )
    {
        (IServerIdProvider ServerId, IAqueductGrainFactory Grains, IStreamSubscriptionManager Streams,
            ISignalRServerDirectoryGrain Directory) fixture = CreateFixture();
        using HeartbeatManager heartbeat = new(
            fixture.ServerId,
            fixture.Grains,
            Options.Create(new AqueductOptions()),
            NullLogger<HeartbeatManager>.Instance);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = new(
            fixture.ServerId,
            fixture.Grains,
            new ConnectionRegistry(),
            Substitute.For<ILocalMessageSender>(),
            heartbeat,
            fixture.Streams,
            Substitute.For<IHostApplicationLifetime>(),
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance);
        OrleansException expectedFailure = new("server directory unavailable");
        fixture.Directory.RegisterServerAsync(heartbeat.ServerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(expectedFailure), Task.CompletedTask);
        HubConnectionContext connection =
            HubConnectionContextFactory.Create(nameof(FailedRegistrationShouldBeRetriedAfterStreamInitialization));
        Func<Task> startup = async () =>
        {
            if (connect)
            {
                await manager.OnConnectedAsync(connection);
            }
            else
            {
                await manager.SendAllAsync("update", ["payload"], TestContext.Current.CancellationToken);
            }
        };
        Exception? failure = await Record.ExceptionAsync(startup);
        Assert.Same(expectedFailure, failure);
        Assert.True(fixture.Streams.IsInitialized);
        await startup();
        await startup();
        await fixture.Directory.Received(2).RegisterServerAsync(heartbeat.ServerId, Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Successful startup retains one-time stream setup and directory registration across broadcasts.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task SuccessfulStartupShouldRemainIdempotent()
    {
        (IServerIdProvider ServerId, IAqueductGrainFactory Grains, IStreamSubscriptionManager Streams,
            ISignalRServerDirectoryGrain Directory) fixture = CreateFixture();
        using HeartbeatManager heartbeat = new(
            fixture.ServerId,
            fixture.Grains,
            Options.Create(new AqueductOptions()),
            NullLogger<HeartbeatManager>.Instance);
        using AqueductHubLifetimeManager<TestAqueductHub> manager = new(
            fixture.ServerId,
            fixture.Grains,
            new ConnectionRegistry(),
            Substitute.For<ILocalMessageSender>(),
            heartbeat,
            fixture.Streams,
            Substitute.For<IHostApplicationLifetime>(),
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance);
        await manager.SendAllAsync("update", ["first"], TestContext.Current.CancellationToken);
        await manager.SendAllAsync("update", ["second"], TestContext.Current.CancellationToken);
        await manager.SendAllAsync("update", ["third"], TestContext.Current.CancellationToken);
        await fixture.Directory.Received(1).RegisterServerAsync(heartbeat.ServerId, Arg.Any<CancellationToken>());
        await fixture.Streams.Received(1)
            .EnsureInitializedAsync(
                nameof(TestAqueductHub),
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Any<Func<AllMessage, Task>>(),
                Arg.Any<CancellationToken>());
        await fixture.Streams.Received(3).PublishToAllAsync(Arg.Any<AllMessage>());
    }
}