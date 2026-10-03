using System;
using System.Collections.Generic;
using System.Linq;
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
        Func<Task> Cleanup, IGrainTimer Timer)> CreateConnectedClientAsync(
        IGrainFactory? sharedFactory = null,
        ISignalRServerLivenessGrain? sharedDirectory = null,
        SignalRServerLivenessCache? sharedCache = null,
        string connectionKey = "hub:connection"
    )
    {
        IGrainContext context = GrainContextMockBuilder.Create().WithGrainKey(connectionKey).BuildObject();
        IGrainRuntime runtime = Substitute.For<IGrainRuntime>();
        IGrainFactory factory = sharedFactory ?? Substitute.For<IGrainFactory>();
        ISignalRServerLivenessGrain directory = sharedDirectory ?? Substitute.For<ISignalRServerLivenessGrain>();
        factory.GetGrain<ISignalRServerLivenessGrain>(SignalRServerDirectoryKey.Default).Returns(directory);
        if (sharedDirectory is null)
        {
            directory.IsServerAliveAsync("server", TimeSpan.FromMinutes(6)).Returns(Task.FromResult(true));
        }

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
        IOptions<AqueductOptions> options = Options.Create(
            new AqueductOptions
            {
                HeartbeatIntervalMinutes = 2,
            });
        SignalRClientGrain client = new(
            context,
            runtime,
            factory,
            sharedCache ?? new SignalRServerLivenessCache(factory, options),
            options,
            NullLogger<SignalRClientGrain>.Instance);
        await client.ConnectAsync("hub", "server");
        Assert.NotNull(callback);
        return (client, directory, () => callback(client, CancellationToken.None), timer);
    }

    /// <summary>
    ///     Many client cleanup callbacks share one pending directory lookup for the same server and timeout.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task ConcurrentClientsShouldShareServerLivenessLookup()
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRServerLivenessGrain directory = Substitute.For<ISignalRServerLivenessGrain>();
        SignalRServerLivenessCache cache = new(
            factory,
            Options.Create(
                new AqueductOptions
                {
                    HeartbeatIntervalMinutes = 2,
                }));
        TaskCompletionSource<bool> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        directory.IsServerAliveAsync("server", TimeSpan.FromMinutes(6)).Returns(response.Task);
        using SignalRClientLifetimes lifetimes = new();
        List<(SignalRClientGrain Client, Func<Task> Cleanup)> clients = [];
        Task? sweep = null;
        try
        {
            for (int index = 0; index < 64; index++)
            {
                (SignalRClientGrain client, ISignalRServerLivenessGrain _, Func<Task> cleanup, IGrainTimer _) =
                    await CreateConnectedClientAsync(factory, directory, cache, $"hub:connection-{index}");
                lifetimes.Add(client);
                clients.Add((client, cleanup));
            }

            directory.ClearReceivedCalls();
            sweep = Task.WhenAll(clients.Select(async client => await client.Cleanup()));
            _ = directory.Received(1).IsServerAliveAsync("server", TimeSpan.FromMinutes(6));
        }
        finally
        {
            response.TrySetResult(true);
            if (sweep is not null)
            {
                await sweep;
            }
        }
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