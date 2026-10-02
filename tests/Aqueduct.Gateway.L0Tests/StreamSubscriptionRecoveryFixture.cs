using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Messages;

using NSubstitute;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Tracks accepted subscriptions and injects failures into stream setup and cleanup.
/// </summary>
internal sealed class StreamSubscriptionRecoveryFixture : IDisposable
{
    private readonly List<IAsyncObserver<ServerMessage>> activeObservers = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="StreamSubscriptionRecoveryFixture" /> class.
    /// </summary>
    public StreamSubscriptionRecoveryFixture()
    {
        IServerIdProvider serverId = Substitute.For<IServerIdProvider>();
        serverId.ServerId.Returns("recovery-server");
        IClusterClient client = Substitute.For<IClusterClient>();
        IStreamProvider provider = Substitute.For<IStreamProvider>();
        IAsyncStream<ServerMessage> serverStream = Substitute.For<IAsyncStream<ServerMessage>>();
        AllStream = Substitute.For<IAsyncStream<AllMessage>>();
        ServiceCollection services = new();
        services.AddKeyedSingleton("recovery-provider", provider);
        Services = services.BuildServiceProvider();
        client.ServiceProvider.Returns(Services);
        provider.GetStream<ServerMessage>(Arg.Any<StreamId>()).Returns(serverStream);
        provider.GetStream<AllMessage>(Arg.Any<StreamId>())
            .Returns(_ => AllLookupFailure is { } failure ? throw failure : AllStream);
        serverStream.SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>())
            .Returns(call =>
            {
                ServerSubscribeCalls++;
                if (ServerSubscribeFailure is { } failure)
                {
                    return Task.FromException<StreamSubscriptionHandle<ServerMessage>>(failure);
                }

                IAsyncObserver<ServerMessage> observer = call.Arg<IAsyncObserver<ServerMessage>>();
                StreamSubscriptionHandle<ServerMessage> handle =
                    Substitute.For<StreamSubscriptionHandle<ServerMessage>>();
                handle.UnsubscribeAsync().Returns(_ => UnsubscribeAsync(observer));
                activeObservers.Add(observer);
                return Task.FromResult(handle);
            });
        AllStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>())
            .Returns(call =>
            {
                AllSubscribeCalls++;
                if (AllSubscribeFailure is { } failure)
                {
                    return Task.FromException<StreamSubscriptionHandle<AllMessage>>(failure);
                }

                AllObserver = call.Arg<IAsyncObserver<AllMessage>>();
                return Task.FromResult(Substitute.For<StreamSubscriptionHandle<AllMessage>>());
            });
        Manager = new(
            serverId,
            client,
            Options.Create(
                new AqueductOptions
                {
                    StreamProviderName = "recovery-provider",
                }),
            Substitute.For<ILogger<StreamSubscriptionManager>>());
    }

    /// <summary>Gets the number of accepted server subscriptions still active.</summary>
    internal int ActiveServerSubscriptions => activeObservers.Count;

    /// <summary>Gets or sets the broadcast lookup failure.</summary>
    internal Exception? AllLookupFailure { get; set; }

    /// <summary>Gets the accepted broadcast observer.</summary>
    internal IAsyncObserver<AllMessage>? AllObserver { get; private set; }

    /// <summary>Gets the broadcast stream used for publication assertions.</summary>
    internal IAsyncStream<AllMessage> AllStream { get; }

    /// <summary>Gets the number of broadcast subscription attempts.</summary>
    internal int AllSubscribeCalls { get; private set; }

    /// <summary>Gets or sets the broadcast subscription failure.</summary>
    internal Exception? AllSubscribeFailure { get; set; }

    /// <summary>Gets or sets an asynchronous barrier before compensation completes.</summary>
    internal Func<Task>? BeforeUnsubscribe { get; set; }

    /// <summary>Gets the production manager under test.</summary>
    internal StreamSubscriptionManager Manager { get; }

    /// <summary>Gets the number of server subscription attempts.</summary>
    internal int ServerSubscribeCalls { get; private set; }

    /// <summary>Gets or sets the server subscription failure.</summary>
    internal Exception? ServerSubscribeFailure { get; set; }

    /// <summary>Gets the number of server unsubscription attempts.</summary>
    internal int UnsubscribeCalls { get; private set; }

    /// <summary>Gets or sets the server unsubscription failure.</summary>
    internal Exception? UnsubscribeFailure { get; set; }

    private ServiceProvider Services { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Manager.Dispose();
        Services.Dispose();
    }

    /// <summary>Delivers a message through each active subscription's actual observer.</summary>
    /// <param name="message">The message to deliver.</param>
    /// <returns>A task representing callback delivery.</returns>
    internal async Task DeliverServerAsync(
        ServerMessage message
    )
    {
        foreach (IAsyncObserver<ServerMessage> observer in activeObservers.ToArray())
        {
            await observer.OnNextAsync(message).ConfigureAwait(false);
        }
    }

    /// <summary>Initializes the manager with the specified callbacks.</summary>
    /// <param name="cancellationToken">The initialization cancellation token.</param>
    /// <param name="onServerMessage">The server callback.</param>
    /// <param name="onAllMessage">The broadcast callback.</param>
    /// <returns>A task representing initialization.</returns>
    internal Task InitializeAsync(
        CancellationToken cancellationToken,
        Func<ServerMessage, Task>? onServerMessage = null,
        Func<AllMessage, Task>? onAllMessage = null
    ) =>
        Manager.EnsureInitializedAsync(
            "RecoveryHub",
            onServerMessage ?? (_ => Task.CompletedTask),
            onAllMessage ?? (_ => Task.CompletedTask),
            cancellationToken);

    private async Task UnsubscribeAsync(
        IAsyncObserver<ServerMessage> observer
    )
    {
        UnsubscribeCalls++;
        if (BeforeUnsubscribe is { } beforeUnsubscribe)
        {
            await beforeUnsubscribe().ConfigureAwait(false);
        }

        if (UnsubscribeFailure is { } failure)
        {
            throw failure;
        }

        activeObservers.Remove(observer);
    }
}