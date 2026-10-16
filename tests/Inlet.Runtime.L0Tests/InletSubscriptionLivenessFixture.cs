using System;
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
///     Owns the dependencies and timer callback for a subscription cleanup test.
/// </summary>
internal sealed class InletSubscriptionLivenessFixture : IDisposable
{
    private Func<InletSubscriptionGrain, CancellationToken, Task>? cleanup;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSubscriptionLivenessFixture" /> class.
    /// </summary>
    public InletSubscriptionLivenessFixture()
    {
        IStreamProvider streamProvider = Substitute.For<IStreamProvider>();
        IAsyncStream<BrookCursorMovedEvent> stream = Substitute.For<IAsyncStream<BrookCursorMovedEvent>>();
        streamProvider.GetStream<BrookCursorMovedEvent>(Arg.Any<StreamId>()).Returns(stream);
        stream.SubscribeAsync(Arg.Any<IAsyncObserver<BrookCursorMovedEvent>>()).Returns(Task.FromResult(Handle));
        Handle.UnsubscribeAsync().Returns(Task.CompletedTask);
        ServiceCollection services = [];
        services.AddKeyedSingleton("cleanup-streams", streamProvider);
        Services = services.BuildServiceProvider();
        IGrainContext context = GrainContextMockBuilder.Create()
            .WithGrainKey("connection")
            .Configure(mock => mock.SetupGet(value => value.ActivationServices).Returns(Services))
            .BuildObject();
        IGrainRuntime runtime = Substitute.For<IGrainRuntime>();
        runtime.ServiceProvider.Returns(Services);
        using IGrainTimer registration = runtime.TimerRegistry.RegisterGrainTimer(
            context,
            Arg.Any<Func<InletSubscriptionGrain, CancellationToken, Task>>(),
            Arg.Any<InletSubscriptionGrain>(),
            Arg.Any<GrainTimerCreationOptions>());
        registration.Returns(call =>
        {
            cleanup = call.Arg<Func<InletSubscriptionGrain, CancellationToken, Task>>();
            return Timer;
        });
        IAqueductGrainFactory aqueduct = Substitute.For<IAqueductGrainFactory>();
        aqueduct.GetClientGrain(InletHubConstants.HubName, "connection").Returns(Client);
        Client.GetServerIdAsync().Returns(Task.FromResult<string?>("server"));
        ProjectionBrookRegistry registry = new();
        registry.Register("projection", "TEST");
        IStreamIdFactory streamIds = Substitute.For<IStreamIdFactory>();
        streamIds.Create(Arg.Any<BrookKey>()).Returns(StreamId.Create("cleanup", "brook"));
        IBrookStorageReader reader = Substitute.For<IBrookStorageReader>();
        reader.ReadCursorPositionAsync(Arg.Any<BrookKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new BrookPosition(0)));
        Grain = new(
            context,
            runtime,
            Substitute.For<IGrainFactory>(),
            aqueduct,
            Options.Create(new AqueductOptions()),
            registry,
            Options.Create(
                new BrookProviderOptions
                {
                    OrleansStreamProviderName = "cleanup-streams",
                }),
            streamIds,
            reader,
            NullLogger<InletSubscriptionGrain>.Instance);
    }

    /// <summary>
    ///     Gets the client route checked by the cleanup callback.
    /// </summary>
    public ISignalRClientGrain Client { get; } = Substitute.For<ISignalRClientGrain>();

    /// <summary>
    ///     Gets the subscription grain owned by this test.
    /// </summary>
    public InletSubscriptionGrain Grain { get; }

    /// <summary>
    ///     Gets the brook observer handle whose release is observed.
    /// </summary>
    public StreamSubscriptionHandle<BrookCursorMovedEvent> Handle { get; } =
        Substitute.For<StreamSubscriptionHandle<BrookCursorMovedEvent>>();

    /// <summary>
    ///     Gets the cleanup timer registration owned by the grain.
    /// </summary>
    public IGrainTimer Timer { get; } = Substitute.For<IGrainTimer>();

    private ServiceProvider Services { get; }

    /// <summary>
    ///     Executes the registered cleanup callback after a successful subscription.
    /// </summary>
    /// <returns>The cleanup operation.</returns>
    public Task CleanupAsync() =>
        cleanup?.Invoke(Grain, CancellationToken.None) ??
        throw new InvalidOperationException("No cleanup timer registered.");

    /// <inheritdoc />
    public void Dispose()
    {
        Grain.Dispose();
        Services.Dispose();
    }
}