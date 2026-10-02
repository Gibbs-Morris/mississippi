using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Aqueduct.Abstractions;
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
            Options.Create(new AqueductOptions()),
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
    ///     Fresh directory state must allow an existing gateway time to restore its heartbeat metadata.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task ReactivatedDirectoryShouldPreserveGatewayUntilNextHeartbeat()
    {
        FakeTimeProvider clock = new();
        SignalRServerDirectoryGrain original = CreateDirectory(clock);
        await original.RegisterServerAsync("surviving-gateway");
        clock.Advance(TimeSpan.FromMinutes(1));
        SignalRServerDirectoryGrain replacement = CreateDirectory(clock);
        Assert.True(await replacement.IsServerAliveAsync("surviving-gateway", TimeSpan.FromMinutes(3)));
    }

    /// <summary>
    ///     A longer requested lease cannot keep unknown registration state beyond the configured recovery window.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task RecoveryWindowShouldExpireAtConfiguredTimeout()
    {
        FakeTimeProvider clock = new();
        SignalRServerDirectoryGrain directory = CreateDirectory(clock);
        Assert.True(await directory.IsServerAliveAsync("missing", TimeSpan.FromHours(1)));
        clock.Advance(TimeSpan.FromMinutes(3));
        Assert.True(await directory.IsServerAliveAsync("missing", TimeSpan.FromHours(1)));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(await directory.IsServerAliveAsync("missing", TimeSpan.FromHours(1)));
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
    ///     Normal heartbeats restore a live registration after the directory loses its volatile state.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task UnknownHeartbeatShouldRestoreGatewayRegistration()
    {
        FakeTimeProvider clock = new();
        SignalRServerDirectoryGrain replacement = CreateDirectory(clock);
        clock.Advance(TimeSpan.FromMinutes(4));
        await replacement.HeartbeatAsync("surviving-gateway", 4);
        Assert.True(await replacement.IsServerAliveAsync("surviving-gateway", TimeSpan.FromMinutes(3)));
        clock.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(
            "surviving-gateway",
            Assert.Single(await replacement.GetDeadServersAsync(TimeSpan.FromMinutes(3))));
    }

    /// <summary>
    ///     Missing registrations cannot retain connections indefinitely.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task UnknownServerShouldNotBeAliveAfterRecoveryWindow()
    {
        FakeTimeProvider clock = new();
        SignalRServerDirectoryGrain directory = CreateDirectory(clock);
        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.False(await directory.IsServerAliveAsync("missing", TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    ///     A late heartbeat cannot immediately resurrect a gateway that explicitly unregistered.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task UnregisteredGatewayShouldRejectLateHeartbeat()
    {
        FakeTimeProvider clock = new();
        SignalRServerDirectoryGrain directory = CreateDirectory(clock);
        await directory.RegisterServerAsync("stopped-gateway");
        await directory.UnregisterServerAsync("stopped-gateway");
        await directory.HeartbeatAsync("stopped-gateway", 4);
        Assert.False(await directory.IsServerAliveAsync("stopped-gateway", TimeSpan.FromMinutes(3)));
        Assert.Empty(await directory.GetDeadServersAsync(TimeSpan.Zero));
        await directory.RegisterServerAsync("stopped-gateway");
        Assert.True(await directory.IsServerAliveAsync("stopped-gateway", TimeSpan.FromMinutes(3)));
    }
}