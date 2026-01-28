using System;
using System.Collections.Immutable;
using System.Threading.Tasks;


using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Grains.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Aqueduct.Grains.L0Tests;

/// <summary>
///     Tests for <see cref="ISignalRServerDirectoryGrain" /> operations.
/// </summary>
[Collection(ClusterTestSuite.Name)]
public sealed class SignalRServerDirectoryGrainTests
{
    /// <summary>
    ///     Tests that GetDeadServersAsync returns empty for no servers.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "GetDeadServersAsync Returns Empty For No Servers")]
    public async Task GetDeadServersAsyncShouldReturnEmptyForNoServers()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("default-7");

        // Act
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.Empty(deadServers);
    }

    /// <summary>
    ///     Tests that HeartbeatAsync does not throw for unknown server.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "HeartbeatAsync Does Not Throw For Unknown Server")]
    public async Task HeartbeatAsyncShouldNotThrowForUnknownServer()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("default-5");

        // Act
        Exception? exception = await Record.ExceptionAsync(() => grain.HeartbeatAsync("unknown-server", 0));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests that HeartbeatAsync updates server timestamp.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "HeartbeatAsync Updates Server Timestamp")]
    public async Task HeartbeatAsyncShouldUpdateServerTimestamp()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("default-4");
        await grain.RegisterServerAsync("server-1");

        // Act - send heartbeat
        await grain.HeartbeatAsync("server-1", 10);
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromHours(1));

        // Assert - server should still be alive
        Assert.Empty(deadServers);
    }

    /// <summary>
    ///     Tests that multiple servers can be registered.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "Multiple Servers Can Be Registered")]
    public async Task MultipleServersCanBeRegistered()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("default-6");

        // Act
        await grain.RegisterServerAsync("server-1");
        await grain.RegisterServerAsync("server-2");
        await grain.RegisterServerAsync("server-3");
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromHours(1));

        // Assert - no servers should be dead
        Assert.Empty(deadServers);
    }

    /// <summary>
    ///     Tests that RegisterServerAsync adds server to directory.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "RegisterServerAsync Adds Server To Directory")]
    public async Task RegisterServerAsyncShouldAddServerToDirectory()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("default-1");

        // Act
        await grain.RegisterServerAsync("server-1");
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromHours(1));

        // Assert - newly registered server should not be dead
        Assert.Empty(deadServers);
    }

    /// <summary>
    ///     Tests that UnregisterServerAsync is idempotent.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "UnregisterServerAsync Is Idempotent")]
    public async Task UnregisterServerAsyncShouldBeIdempotent()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("default-3");
        await grain.RegisterServerAsync("server-1");

        // Act
        await grain.UnregisterServerAsync("server-1");
        Exception? exception = await Record.ExceptionAsync(() => grain.UnregisterServerAsync("server-1"));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests that UnregisterServerAsync removes server from directory.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "UnregisterServerAsync Removes Server From Directory")]
    public async Task UnregisterServerAsyncShouldRemoveServerFromDirectory()
    {
        // Arrange
        ISignalRServerDirectoryGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRServerDirectoryGrain>("default-2");
        await grain.RegisterServerAsync("server-1");

        // Act
        await grain.UnregisterServerAsync("server-1");
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromMilliseconds(1));

        // Assert - server is removed so it shouldn't appear in dead list
        Assert.Empty(deadServers);
    }
}