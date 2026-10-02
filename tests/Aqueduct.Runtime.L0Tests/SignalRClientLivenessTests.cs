using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Keys;
using Mississippi.Aqueduct.Runtime.Grains;
using Mississippi.Testing.Utilities.Mocks;

using NSubstitute;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Verifies that cleanup distinguishes a dead server from an unavailable liveness query.
/// </summary>
public sealed class SignalRClientLivenessTests
{
    private static async Task<(SignalRClientGrain Client, ISignalRServerLivenessGrain Directory,
        Func<Task> Cleanup, IGrainTimer Timer)> CreateConnectedClientAsync()
    {
        IGrainContext context = GrainContextMockBuilder.Create().WithGrainKey("hub:connection").BuildObject();
        IGrainRuntime runtime = Substitute.For<IGrainRuntime>();
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRServerLivenessGrain directory = Substitute.For<ISignalRServerLivenessGrain>();
        factory.GetGrain<ISignalRServerLivenessGrain>(SignalRServerDirectoryKey.Default).Returns(directory);
        directory.IsServerAliveAsync("server", TimeSpan.FromMinutes(6)).Returns(Task.FromResult(true));
        IGrainTimer timer = Substitute.For<IGrainTimer>();
        Func<SignalRClientGrain, CancellationToken, Task>? callback = null;
        using IGrainTimer registration = runtime.TimerRegistry.RegisterGrainTimer(
            context,
            Arg.Any<Func<SignalRClientGrain, CancellationToken, Task>>(),
            Arg.Any<SignalRClientGrain>(),
            Arg.Any<GrainTimerCreationOptions>());
        registration.Returns(call =>
        {
            callback = call.Arg<Func<SignalRClientGrain, CancellationToken, Task>>();
            return timer;
        });
        SignalRClientGrain client = new(
            context,
            runtime,
            factory,
            Options.Create(
                new AqueductOptions
                {
                    HeartbeatIntervalMinutes = 2,
                }),
            NullLogger<SignalRClientGrain>.Instance);
        await client.ConnectAsync("hub", "server");
        Assert.NotNull(callback);
        return (client, directory, () => callback(client, CancellationToken.None), timer);
    }

    /// <summary>
    ///     A successful heartbeat check retains an otherwise idle connection.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task HealthyServerShouldRetainConnectedState()
    {
        (SignalRClientGrain client, ISignalRServerLivenessGrain directory, Func<Task> cleanup, IGrainTimer timer) =
            await CreateConnectedClientAsync();
        using IDisposable clientLifetime = client;
        await cleanup();
        Assert.Equal("server", await client.GetServerIdAsync());
        await directory.Received(1).IsServerAliveAsync("server", TimeSpan.FromMinutes(6));
        timer.DidNotReceive().Dispose();
        await client.DisconnectAsync();
        timer.Received(1).Dispose();
    }

    /// <summary>
    ///     Failed liveness requests preserve state until a later definitive dead-server response.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task LivenessFailureShouldPreserveStateAndAllowRetry()
    {
        (SignalRClientGrain client, ISignalRServerLivenessGrain directory, Func<Task> cleanup, IGrainTimer timer) =
            await CreateConnectedClientAsync();
        using IDisposable clientLifetime = client;
        directory.IsServerAliveAsync("server", TimeSpan.FromMinutes(6))
            .Returns(Task.FromException<bool>(new OrleansException("directory unavailable")));
        await Assert.ThrowsAsync<OrleansException>(cleanup);
        Assert.Equal("server", await client.GetServerIdAsync());
        timer.DidNotReceive().Dispose();
        directory.IsServerAliveAsync("server", TimeSpan.FromMinutes(6)).Returns(Task.FromResult(false));
        await cleanup();
        Assert.Null(await client.GetServerIdAsync());
        timer.Received(1).Dispose();
    }
}