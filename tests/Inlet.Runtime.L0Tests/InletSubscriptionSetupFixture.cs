using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Runtime.Grains;
using Mississippi.Testing.Utilities.Mocks;

using NSubstitute;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Inlet.Runtime.L0Tests;

/// <summary>
///     Owns deterministic dependencies for initial subscription setup failures.
/// </summary>
internal sealed class InletSubscriptionSetupFixture : IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSubscriptionSetupFixture" /> class.
    /// </summary>
    public InletSubscriptionSetupFixture()
    {
        RestoreSetup();
        Handle.UnsubscribeAsync().Returns(Task.CompletedTask);
        Client.SendMessageAsync(Arg.Any<string>(), Arg.Any<ImmutableArray<object?>>()).Returns(Task.CompletedTask);
        IStreamProvider provider = Substitute.For<IStreamProvider>();
        provider.GetStream<BrookCursorMovedEvent>(Arg.Any<StreamId>()).Returns(Stream);
        ServiceCollection services = [];
        services.AddKeyedSingleton("setup-streams", provider);
        Services = services.BuildServiceProvider();
        IGrainContext context = GrainContextMockBuilder.Create()
            .WithGrainKey("connection")
            .Configure(mock => mock.SetupGet(value => value.ActivationServices).Returns(Services))
            .BuildObject();
        IAqueductGrainFactory aqueduct = Substitute.For<IAqueductGrainFactory>();
        aqueduct.GetClientGrain(InletHubConstants.HubName, "connection").Returns(Client);
        ProjectionBrookRegistry registry = new();
        registry.Register("projection", "TEST");
        registry.Register("alternate", "TEST");
        registry.Register("other", "OTHER");
        IStreamIdFactory streamIds = Substitute.For<IStreamIdFactory>();
        streamIds.Create(Arg.Any<BrookKey>())
            .Returns(call => StreamId.Create("setup", call.Arg<BrookKey>().ToString()));
        Grain = new(
            context,
            Substitute.For<IGrainFactory>(),
            aqueduct,
            registry,
            Options.Create(
                new BrookProviderOptions
                {
                    OrleansStreamProviderName = "setup-streams",
                }),
            streamIds,
            Reader,
            NullLogger<InletSubscriptionGrain>.Instance);
    }

    /// <summary>
    ///     Gets the client whose notification count is observed.
    /// </summary>
    public ISignalRClientGrain Client { get; } = Substitute.For<ISignalRClientGrain>();

    /// <summary>
    ///     Gets the subscription grain under test.
    /// </summary>
    public InletSubscriptionGrain Grain { get; }

    /// <summary>
    ///     Gets the successful brook handle whose release is observed.
    /// </summary>
    public StreamSubscriptionHandle<BrookCursorMovedEvent> Handle { get; } =
        Substitute.For<StreamSubscriptionHandle<BrookCursorMovedEvent>>();

    /// <summary>
    ///     Gets the dependency which reads the initial cursor.
    /// </summary>
    public IBrookStorageReader Reader { get; } = Substitute.For<IBrookStorageReader>();

    /// <summary>
    ///     Gets the dependency which establishes the first brook observer.
    /// </summary>
    public IAsyncStream<BrookCursorMovedEvent> Stream { get; } = Substitute.For<IAsyncStream<BrookCursorMovedEvent>>();

    private ServiceProvider Services { get; }

    /// <inheritdoc />
    public void Dispose() => Services.Dispose();

    /// <summary>
    ///     Makes one of the two pre-handle setup dependencies fail deterministically.
    /// </summary>
    /// <param name="failCursor">Whether the cursor read fails rather than the stream subscription.</param>
    public void FailSetup(
        bool failCursor
    )
    {
        if (failCursor)
        {
            Reader.ReadCursorPositionAsync(Arg.Any<BrookKey>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<BrookPosition>(new InvalidOperationException("cursor setup failed")));
        }
        else
        {
            Stream.SubscribeAsync(Arg.Any<IAsyncObserver<BrookCursorMovedEvent>>())
                .Returns(
                    Task.FromException<StreamSubscriptionHandle<BrookCursorMovedEvent>>(
                        new InvalidOperationException("stream setup failed")));
        }
    }

    /// <summary>
    ///     Restores successful setup with an initial cursor position of three.
    /// </summary>
    public void RestoreSetup()
    {
        Reader.ReadCursorPositionAsync(Arg.Any<BrookKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new BrookPosition(3)));
        Stream.SubscribeAsync(Arg.Any<IAsyncObserver<BrookCursorMovedEvent>>()).Returns(Task.FromResult(Handle));
    }
}