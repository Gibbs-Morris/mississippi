using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Messages;
using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Owns production backplane registrations while capturing only external stream and client writes.
/// </summary>
internal sealed class AqueductMultipleHubFixture : IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AqueductMultipleHubFixture" /> class.
    /// </summary>
    /// <param name="configureOptions">Whether to exercise the registration overload with options.</param>
    public AqueductMultipleHubFixture(
        bool configureOptions = false
    )
    {
        IClusterClient cluster = Substitute.For<IClusterClient>();
        IStreamProvider streams = Substitute.For<IStreamProvider>();
        StreamSubscriptionHandle<ServerMessage> serverHandle =
            Substitute.For<StreamSubscriptionHandle<ServerMessage>>();
        IAsyncStream<ServerMessage> serverStream = Substitute.For<IAsyncStream<ServerMessage>>();
        serverStream.SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>())
            .Returns(call =>
            {
                ServerObservers.Add(call.Arg<IAsyncObserver<ServerMessage>>());
                return Task.FromResult(serverHandle);
            });
        streams.GetStream<ServerMessage>(Arg.Any<StreamId>()).Returns(serverStream);
        streams.GetStream<AllMessage>(Arg.Any<StreamId>()).Returns(call => GetAllStream(call.Arg<StreamId>()));
        Grains.GetClientGrain(Arg.Any<string>(), Arg.Any<string>()).Returns(Client);
        Heartbeat.StartAsync(Arg.Any<Func<int>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                FirstHeartbeatCountProvider ??= call.Arg<Func<int>>();
                return Task.CompletedTask;
            });
        ServiceCollection services = new();
        services.AddLogging();
        services.AddOptions<AqueductOptions>();
        services.AddSingleton(cluster);
        services.AddSingleton(Grains);
        services.AddSingleton(Sender);
        services.AddSingleton(Heartbeat);
        services.AddKeyedSingleton(AqueductStreamDefaults.StreamProviderName, streams);
        if (configureOptions)
        {
            services.AddAqueduct<TestAqueductHub>(_ => { });
            services.AddAqueduct<SecondAqueductHub>(_ => { });
        }
        else
        {
            services.AddAqueduct<TestAqueductHub>();
            services.AddAqueduct<SecondAqueductHub>();
        }

        Services = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        cluster.ServiceProvider.Returns(Services);
        FirstManager = Services.GetRequiredService<HubLifetimeManager<TestAqueductHub>>();
        SecondManager = Services.GetRequiredService<HubLifetimeManager<SecondAqueductHub>>();
        Assert.IsType<AqueductHubLifetimeManager<TestAqueductHub>>(FirstManager);
        Assert.IsType<AqueductHubLifetimeManager<SecondAqueductHub>>(SecondManager);
    }

    /// <summary>
    ///     Gets the captured broadcast callback for each actual Orleans stream.
    /// </summary>
    public Dictionary<StreamId, IAsyncObserver<AllMessage>> AllObservers { get; } = new();

    /// <summary>
    ///     Gets the boundary grain used for connection operations.
    /// </summary>
    public ISignalRClientGrain Client { get; } = Substitute.For<ISignalRClientGrain>();

    /// <summary>
    ///     Gets the first hub's real connection context.
    /// </summary>
    public HubConnectionContext FirstConnection { get; } = HubConnectionContextFactory.Create("first-hub-client");

    /// <summary>
    ///     Gets the count provider accepted by the shared heartbeat's first startup.
    /// </summary>
    public Func<int>? FirstHeartbeatCountProvider { get; private set; }

    /// <summary>
    ///     Gets the lifetime manager resolved through production DI for the first hub.
    /// </summary>
    public HubLifetimeManager<TestAqueductHub> FirstManager { get; }

    /// <summary>
    ///     Gets the grain-resolution boundary.
    /// </summary>
    public IAqueductGrainFactory Grains { get; } = Substitute.For<IAqueductGrainFactory>();

    /// <summary>
    ///     Gets the shared heartbeat boundary, whose lifetime belongs to this fixture.
    /// </summary>
    public IHeartbeatManager Heartbeat { get; } = Substitute.For<IHeartbeatManager>();

    /// <summary>
    ///     Gets the stream identifiers used by actual broadcast publication.
    /// </summary>
    public List<StreamId> PublishedStreams { get; } = new();

    /// <summary>
    ///     Gets the second hub's real connection context.
    /// </summary>
    public HubConnectionContext SecondConnection { get; } = HubConnectionContextFactory.Create("second-hub-client");

    /// <summary>
    ///     Gets the lifetime manager resolved through production DI for the second hub.
    /// </summary>
    public HubLifetimeManager<SecondAqueductHub> SecondManager { get; }

    /// <summary>
    ///     Gets the final SignalR write boundary.
    /// </summary>
    public ILocalMessageSender Sender { get; } = Substitute.For<ILocalMessageSender>();

    /// <summary>
    ///     Gets callbacks registered on the common server stream.
    /// </summary>
    public List<IAsyncObserver<ServerMessage>> ServerObservers { get; } = new();

    private Dictionary<StreamId, IAsyncStream<AllMessage>> AllStreams { get; } = new();

    private ServiceProvider Services { get; }

    /// <summary>
    ///     Returns the production stream identifier for a hub's broadcast channel.
    /// </summary>
    /// <param name="hubName">The hub's routing name.</param>
    /// <returns>The hub-specific stream identifier.</returns>
    public static StreamId BroadcastId(
        string hubName
    ) =>
        StreamId.Create(AqueductStreamDefaults.AllClientsStreamNamespace, hubName);

    /// <summary>
    ///     Connects both hubs in the selected initialization order.
    /// </summary>
    /// <param name="reverse">Whether the second hub connects first.</param>
    /// <returns>The connection operation.</returns>
    public async Task ConnectBothAsync(
        bool reverse = false
    )
    {
        if (reverse)
        {
            await SecondManager.OnConnectedAsync(SecondConnection);
            await FirstManager.OnConnectedAsync(FirstConnection);
        }
        else
        {
            await FirstManager.OnConnectedAsync(FirstConnection);
            await SecondManager.OnConnectedAsync(SecondConnection);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Services.Dispose();
        Heartbeat.Dispose();
    }

    /// <summary>
    ///     Returns the captured broadcast stream for the specified routing channel.
    /// </summary>
    /// <param name="id">The production stream identifier for the channel.</param>
    /// <returns>The stream substitute that captures callbacks and publications.</returns>
    private IAsyncStream<AllMessage> GetAllStream(
        StreamId id
    )
    {
        if (AllStreams.TryGetValue(id, out IAsyncStream<AllMessage>? existing))
        {
            return existing;
        }

        IAsyncStream<AllMessage> stream = Substitute.For<IAsyncStream<AllMessage>>();
        StreamSubscriptionHandle<AllMessage> handle = Substitute.For<StreamSubscriptionHandle<AllMessage>>();
        stream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>())
            .Returns(call =>
            {
                AllObservers.Add(id, call.Arg<IAsyncObserver<AllMessage>>());
                return Task.FromResult(handle);
            });
        stream.OnNextAsync(Arg.Any<AllMessage>())
            .Returns(_ =>
            {
                PublishedStreams.Add(id);
                return Task.CompletedTask;
            });
        AllStreams.Add(id, stream);
        return stream;
    }
}