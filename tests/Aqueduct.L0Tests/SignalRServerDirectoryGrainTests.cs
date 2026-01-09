using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="ISignalRServerDirectoryGrain" /> operations.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("SignalR Orleans")]
[AllureSubSuite("Server Directory Grain")]
[Collection(ClusterTestSuite.Name)]
public sealed class SignalRServerDirectoryGrainTests
{
    /// <summary>
    ///     Tests that a server can be registered.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "RegisterServerAsync Registers Server")]
    public async Task RegisterServerAsyncShouldRegisterServer()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("test-directory-1");

        // Act
        await grain.RegisterServerAsync("server1");

        // Assert - server should be registered (check via dead servers with zero timeout)
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.Zero);
        Assert.Contains("server1", deadServers);
    }

    /// <summary>
    ///     Tests that registering same server twice is idempotent.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "RegisterServerAsync Is Idempotent")]
    public async Task RegisterServerAsyncShouldBeIdempotent()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("test-directory-2");

        // Act
        await grain.RegisterServerAsync("server-idem");
        await grain.RegisterServerAsync("server-idem");

        // Assert - should not throw and server exists
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.Zero);
        Assert.Single(deadServers);
    }

    /// <summary>
    ///     Tests that a server can be unregistered.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "UnregisterServerAsync Unregisters Server")]
    public async Task UnregisterServerAsyncShouldUnregisterServer()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("test-directory-3");
        await grain.RegisterServerAsync("server-unreg");

        // Act
        await grain.UnregisterServerAsync("server-unreg");

        // Assert - server should not be in dead list (because it's gone)
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.Zero);
        Assert.DoesNotContain("server-unreg", deadServers);
    }

    /// <summary>
    ///     Tests that unregistering a non-existent server is a no-op.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "UnregisterServerAsync Is No-Op For Non-Existent Server")]
    public async Task UnregisterServerAsyncShouldBeNoOpForNonExistentServer()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("test-directory-4");

        // Act
        Exception? exception = await Record.ExceptionAsync(() => grain.UnregisterServerAsync("nonexistent-server"));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests that heartbeat updates server state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "HeartbeatAsync Updates Server State")]
    public async Task HeartbeatAsyncShouldUpdateServerState()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("test-directory-5");
        await grain.RegisterServerAsync("server-heartbeat");

        // Act
        await grain.HeartbeatAsync("server-heartbeat", 5);

        // Assert - server should not be dead with large timeout
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromHours(1));
        Assert.DoesNotContain("server-heartbeat", deadServers);
    }

    /// <summary>
    ///     Tests that heartbeat for unknown server is ignored.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "HeartbeatAsync Is Ignored For Unknown Server")]
    public async Task HeartbeatAsyncShouldBeIgnoredForUnknownServer()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("test-directory-6");

        // Act
        Exception? exception = await Record.ExceptionAsync(() => grain.HeartbeatAsync("unknown-server", 10));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests that GetDeadServersAsync returns empty for recently heartbeated servers.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "GetDeadServersAsync Returns Empty For Fresh Servers")]
    public async Task GetDeadServersAsyncShouldReturnEmptyForFreshServers()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("test-directory-7");
        await grain.RegisterServerAsync("fresh-server");
        await grain.HeartbeatAsync("fresh-server", 1);

        // Act
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromMinutes(5));

        // Assert
        Assert.DoesNotContain("fresh-server", deadServers);
    }

    /// <summary>
    ///     Tests that GetDeadServersAsync returns servers that haven't sent heartbeat.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "GetDeadServersAsync Returns Stale Servers")]
    public async Task GetDeadServersAsyncShouldReturnStaleServers()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("test-directory-8");
        await grain.RegisterServerAsync("stale-server");

        // Act - use zero timeout so any server is considered stale
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.Zero);

        // Assert
        Assert.Contains("stale-server", deadServers);
    }

    /// <summary>
    ///     Tests that multiple servers can be registered and tracked.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "Multiple Servers Can Be Registered")]
    public async Task MultipleServersShouldBeRegistered()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("test-directory-9");

        // Act
        await grain.RegisterServerAsync("multi-server-1");
        await grain.RegisterServerAsync("multi-server-2");
        await grain.RegisterServerAsync("multi-server-3");

        // Assert
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.Zero);
        Assert.Equal(3, deadServers.Count);
    }
}
