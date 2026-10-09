using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Keys;
using Mississippi.Aqueduct.Runtime.Grains;

using NSubstitute;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Verifies bounded reuse, retry, and server-specific isolation of silo-wide liveness queries.
/// </summary>
public sealed class SignalRServerLivenessCacheTests
{
    /// <summary>
    ///     A failed query cannot remain cached and prevent the next cleanup from retrying.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task FailedQueryShouldAllowImmediateRetry()
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRServerLivenessGrain directory = Substitute.For<ISignalRServerLivenessGrain>();
        factory.GetGrain<ISignalRServerLivenessGrain>(SignalRServerDirectoryKey.Default).Returns(directory);
        TimeSpan timeout = TimeSpan.FromMinutes(3);
        directory.IsServerAliveAsync("server", timeout)
            .Returns(Task.FromException<bool>(new OrleansException("directory unavailable")), Task.FromResult(false));
        SignalRServerLivenessCache cache = new(factory, Options.Create(new AqueductOptions()));
        await Assert.ThrowsAsync<OrleansException>(() => cache.IsServerAliveAsync("server", timeout));
        Assert.False(await cache.IsServerAliveAsync("server", timeout));
        await directory.Received(2).IsServerAliveAsync("server", timeout);
    }

    /// <summary>
    ///     Different servers and accepted heartbeat ages require independent directory responses.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task ServerAndTimeoutShouldIdentifyIndependentQueries()
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRServerLivenessGrain directory = Substitute.For<ISignalRServerLivenessGrain>();
        factory.GetGrain<ISignalRServerLivenessGrain>(SignalRServerDirectoryKey.Default).Returns(directory);
        directory.IsServerAliveAsync(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns(Task.FromResult(true));
        SignalRServerLivenessCache cache = new(factory, Options.Create(new AqueductOptions()));
        Assert.True(await cache.IsServerAliveAsync("one", TimeSpan.FromMinutes(3)));
        Assert.True(await cache.IsServerAliveAsync("one", TimeSpan.FromMinutes(6)));
        Assert.True(await cache.IsServerAliveAsync("two", TimeSpan.FromMinutes(3)));
        Assert.True(await cache.IsServerAliveAsync("one", TimeSpan.FromMinutes(3)));
        await directory.Received(1).IsServerAliveAsync("one", TimeSpan.FromMinutes(3));
        await directory.Received(1).IsServerAliveAsync("one", TimeSpan.FromMinutes(6));
        await directory.Received(1).IsServerAliveAsync("two", TimeSpan.FromMinutes(3));
    }

    /// <summary>
    ///     A successful response expires at one heartbeat interval, allowing dead-server cleanup to proceed.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task SuccessfulQueryShouldExpireAtHeartbeatInterval()
    {
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        ISignalRServerLivenessGrain directory = Substitute.For<ISignalRServerLivenessGrain>();
        factory.GetGrain<ISignalRServerLivenessGrain>(SignalRServerDirectoryKey.Default).Returns(directory);
        FakeTimeProvider clock = new();
        TimeSpan timeout = TimeSpan.FromMinutes(6);
        directory.IsServerAliveAsync("server", timeout).Returns(Task.FromResult(true), Task.FromResult(false));
        SignalRServerLivenessCache cache = new(
            factory,
            Options.Create(
                new AqueductOptions
                {
                    HeartbeatIntervalMinutes = 2,
                }),
            clock);
        Assert.True(await cache.IsServerAliveAsync("server", timeout));
        clock.Advance(TimeSpan.FromSeconds(119));
        Assert.True(await cache.IsServerAliveAsync("server", timeout));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(await cache.IsServerAliveAsync("server", timeout));
        await directory.Received(2).IsServerAliveAsync("server", timeout);
    }
}