using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Messages;
using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>Owns a real gateway, native server subscription and its test connections.</summary>
internal sealed class UserRoutingGateway : IAsyncDisposable
{
    private readonly List<HubConnectionContext> connections = [];

    private readonly ConcurrentQueue<(string ConnectionId, string Method)> deliveries = new();

    private readonly SemaphoreSlim deliverySignal = new(0);

    private readonly string markerConnectionId = Guid.NewGuid().ToString("N");

    private readonly IAsyncStream<ServerMessage> serverStream;

    private Func<ServerMessage, Task>? serverCallback;

    private StreamSubscriptionHandle<ServerMessage>? subscription;

    /// <summary>Initializes a new instance of the <see cref="UserRoutingGateway" /> class.</summary>
    /// <param name="client">The existing test cluster client.</param>
    internal UserRoutingGateway(
        IClusterClient client
    )
    {
        IServerIdProvider server = Substitute.For<IServerIdProvider>();
        server.ServerId.Returns(Guid.NewGuid().ToString("N"));
        serverStream = client.GetStreamProvider(AqueductStreamDefaults.StreamProviderName)
            .GetStream<ServerMessage>(StreamId.Create(AqueductStreamDefaults.ServerStreamNamespace, server.ServerId));
        IStreamSubscriptionManager subscriptions = Substitute.For<IStreamSubscriptionManager>();
        subscriptions.IsInitialized.Returns(true);
        subscriptions.EnsureInitializedAsync(
                Arg.Any<string>(),
                Arg.Any<Func<ServerMessage, Task>>(),
                Arg.Any<Func<AllMessage, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                serverCallback = call.ArgAt<Func<ServerMessage, Task>>(1);
                return Task.CompletedTask;
            });
        ILocalMessageSender sender = Substitute.For<ILocalMessageSender>();
        sender.SendAsync(Arg.Any<HubConnectionContext>(), Arg.Any<string>(), Arg.Any<object?[]>())
            .Returns(call =>
            {
                deliveries.Enqueue((call.Arg<HubConnectionContext>().ConnectionId, call.Arg<string>()));
                deliverySignal.Release();
                return Task.CompletedTask;
            });
        Manager = new(
            server,
            new AqueductGrainFactory(client, NullLogger<AqueductGrainFactory>.Instance),
            new ConnectionRegistry(),
            sender,
            Substitute.For<IHeartbeatManager>(),
            subscriptions,
            NullLogger<AqueductHubLifetimeManager<TestAqueductHub>>.Instance);
    }

    /// <summary>Gets the production lifetime manager.</summary>
    internal AqueductHubLifetimeManager<TestAqueductHub> Manager { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        using (deliverySignal)
        using (Manager)
        {
            try
            {
                foreach (HubConnectionContext connection in connections)
                {
                    await Manager.OnDisconnectedAsync(connection).ConfigureAwait(false);
                }
            }
            finally
            {
                if (subscription is not null)
                {
                    await subscription.UnsubscribeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>Registers a connection with the specified identity.</summary>
    /// <param name="userId">The identity, or null for an anonymous connection.</param>
    /// <returns>The registered connection.</returns>
    internal async Task<HubConnectionContext> ConnectAsync(
        string? userId
    )
    {
        HubConnectionContext connection = HubConnectionContextFactory.Create(Guid.NewGuid().ToString("N"));
        connection.UserIdentifier = userId;
        connections.Add(connection);
        await Manager.OnConnectedAsync(connection).ConfigureAwait(false);
        return connection;
    }

    /// <summary>Disconnects and removes an owned connection.</summary>
    /// <param name="connection">The connection to disconnect.</param>
    /// <returns>The cleanup operation.</returns>
    internal async Task DisconnectAsync(
        HubConnectionContext connection
    )
    {
        await Manager.OnDisconnectedAsync(connection).ConfigureAwait(false);
        connections.Remove(connection);
    }

    /// <summary>Awaits a barrier on the same server stream before inspecting recipients.</summary>
    /// <returns>The barrier operation.</returns>
    internal async Task FlushAsync()
    {
        string marker = "barrier-" + Guid.NewGuid().ToString("N");
        await serverStream.OnNextAsync(
                new()
                {
                    ConnectionId = markerConnectionId,
                    MethodName = marker,
                    Args = [],
                })
            .ConfigureAwait(false);
        while (!deliveries.Any(delivery => delivery.Method == marker))
        {
            if (!await deliverySignal.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken)
                    .ConfigureAwait(false))
            {
                throw new TimeoutException("The native server-stream delivery barrier did not complete.");
            }
        }
    }

    /// <summary>Gets the connections that actually received a method through the gateway callback.</summary>
    /// <param name="method">The method to inspect.</param>
    /// <returns>The delivered connection identifiers.</returns>
    internal string[] GetRecipients(
        string method
    ) =>
        deliveries.Where(delivery => delivery.Method == method)
            .Select(delivery => delivery.ConnectionId)
            .Order()
            .ToArray();

    /// <summary>Subscribes to the actual native server stream using the manager's callback.</summary>
    /// <returns>The initialization operation.</returns>
    internal async Task InitializeAsync()
    {
        if (subscription is not null)
        {
            throw new InvalidOperationException("The test gateway is already initialized.");
        }

        HubConnectionContext marker = HubConnectionContextFactory.Create(markerConnectionId);
        connections.Add(marker);
        await Manager.OnConnectedAsync(marker).ConfigureAwait(false);
        Func<ServerMessage, Task> callback =
            serverCallback ?? throw new InvalidOperationException("Missing gateway callback.");
        subscription = await serverStream.SubscribeAsync((message, _) => callback(message)).ConfigureAwait(false);
    }
}