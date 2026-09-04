using System;
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
///     Controls stream subscription completion and cleanup without an Orleans cluster.
/// </summary>
internal sealed class StreamSubscriptionFixture : IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StreamSubscriptionFixture" /> class.
    /// </summary>
    public StreamSubscriptionFixture()
    {
        Logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        IClusterClient cluster = Substitute.For<IClusterClient>();
        IStreamProvider streams = Substitute.For<IStreamProvider>();
        AqueductOptions options = new();
        ServiceCollection services = new();
        services.AddKeyedSingleton(options.StreamProviderName, streams);
        Services = services.BuildServiceProvider();
        cluster.ServiceProvider.Returns(Services);
        streams.GetStream<ServerMessage>(Arg.Any<StreamId>()).Returns(ServerStream);
        streams.GetStream<AllMessage>(Arg.Any<StreamId>()).Returns(AllStream);
        ServerStream.SubscribeAsync(Arg.Any<IAsyncObserver<ServerMessage>>()).Returns(Task.FromResult(ServerHandle));
        AllStream.SubscribeAsync(Arg.Any<IAsyncObserver<AllMessage>>()).Returns(Task.FromResult(AllHandle));
        IServerIdProvider serverId = Substitute.For<IServerIdProvider>();
        serverId.ServerId.Returns("test-server");
        Manager = new(serverId, cluster, Options.Create(options), Logger);
    }

    /// <summary>Gets the broadcast subscription handle.</summary>
    public StreamSubscriptionHandle<AllMessage> AllHandle { get; } =
        Substitute.For<StreamSubscriptionHandle<AllMessage>>();

    /// <summary>Gets the broadcast stream.</summary>
    public IAsyncStream<AllMessage> AllStream { get; } = Substitute.For<IAsyncStream<AllMessage>>();

    /// <summary>Gets the logger for asserting cleanup diagnostics.</summary>
    public ILogger<StreamSubscriptionManager> Logger { get; } = Substitute.For<ILogger<StreamSubscriptionManager>>();

    /// <summary>Gets the manager under test.</summary>
    public StreamSubscriptionManager Manager { get; }

    /// <summary>Gets the targeted subscription handle.</summary>
    public StreamSubscriptionHandle<ServerMessage> ServerHandle { get; } =
        Substitute.For<StreamSubscriptionHandle<ServerMessage>>();

    /// <summary>Gets the targeted stream.</summary>
    public IAsyncStream<ServerMessage> ServerStream { get; } = Substitute.For<IAsyncStream<ServerMessage>>();

    private ServiceProvider Services { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Manager.Dispose();
        Services.Dispose();
    }

    /// <summary>Initializes the subscriptions with inert message handlers.</summary>
    /// <param name="cancellationToken">The token controlling the initialization wait.</param>
    /// <returns>The initialization task.</returns>
    public Task InitializeAsync(
        CancellationToken cancellationToken = default
    ) =>
        Manager.EnsureInitializedAsync("TestHub", _ => Task.CompletedTask, _ => Task.CompletedTask, cancellationToken);
}