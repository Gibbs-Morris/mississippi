using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Aqueduct.Runtime.Grains;
using Mississippi.Testing.Utilities.Mocks;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Verifies the directory evidence used to release orphaned connection state.
/// </summary>
public sealed class SignalRServerLivenessTests
{
    private static SignalRServerDirectoryGrain CreateDirectory(
        FakeTimeProvider clock
    ) =>
        new(
            GrainContextMockBuilder.Create().WithGrainKey("default").BuildObject(),
            NullLogger<SignalRServerDirectoryGrain>.Instance,
            clock);

    /// <summary>
    ///     Heartbeats refresh an expired registration and unregistration removes liveness.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task HeartbeatAndUnregisterShouldChangeLiveness()
    {
        FakeTimeProvider clock = new();
        SignalRServerDirectoryGrain directory = CreateDirectory(clock);
        await directory.RegisterServerAsync("server");
        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.False(await directory.IsServerAliveAsync("server", TimeSpan.FromMinutes(1)));
        await directory.HeartbeatAsync("server", 3);
        Assert.True(await directory.IsServerAliveAsync("server", TimeSpan.FromMinutes(1)));
        await directory.UnregisterServerAsync("server");
        Assert.False(await directory.IsServerAliveAsync("server", TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    ///     A server is live through the exact deadline and expired only after it.
    /// </summary>
    /// <param name="elapsedSeconds">The elapsed time since registration.</param>
    /// <param name="expectedAlive">Whether the deadline still includes the heartbeat.</param>
    /// <returns>The test operation.</returns>
    [Theory]
    [InlineData(0, true)]
    [InlineData(59, true)]
    [InlineData(60, true)]
    [InlineData(61, false)]
    public async Task RegistrationShouldRespectHeartbeatDeadline(
        int elapsedSeconds,
        bool expectedAlive
    )
    {
        FakeTimeProvider clock = new();
        SignalRServerDirectoryGrain directory = CreateDirectory(clock);
        await directory.RegisterServerAsync("server");
        clock.Advance(TimeSpan.FromSeconds(elapsedSeconds));
        Assert.Equal(expectedAlive, await directory.IsServerAliveAsync("server", TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    ///     Missing registrations cannot retain connections indefinitely.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task UnknownServerShouldNotBeAlive()
    {
        SignalRServerDirectoryGrain directory = CreateDirectory(new());
        Assert.False(await directory.IsServerAliveAsync("missing", TimeSpan.FromMinutes(1)));
    }
}