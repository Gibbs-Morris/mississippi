using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="ISignalRClientGrain" /> operations.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("SignalR Orleans")]
[AllureSubSuite("Client Grain")]
[Collection(ClusterTestSuite.Name)]
public sealed class SignalRClientGrainTests
{
    /// <summary>
    ///     Tests that a client grain can connect and track server.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "ConnectAsync Sets Server Id")]
    public async Task ConnectAsyncShouldSetServerId()
    {
        // Arrange
        ISignalRClientGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRClientGrain>("TestHub:conn1");

        // Act
        await grain.ConnectAsync("TestHub", "server1");
        string? serverId = await grain.GetServerIdAsync();

        // Assert
        Assert.Equal("server1", serverId);
    }

    /// <summary>
    ///     Tests that connect can be called multiple times to update server.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "ConnectAsync Can Update Server")]
    public async Task ConnectAsyncShouldUpdateServer()
    {
        // Arrange
        ISignalRClientGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRClientGrain>("TestHub:conn3");
        await grain.ConnectAsync("TestHub", "server1");

        // Act
        await grain.ConnectAsync("TestHub", "server2");
        string? serverId = await grain.GetServerIdAsync();

        // Assert
        Assert.Equal("server2", serverId);
    }

    /// <summary>
    ///     Tests that disconnect after disconnect is a no-op.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "DisconnectAsync Is Idempotent")]
    public async Task DisconnectAsyncShouldBeIdempotent()
    {
        // Arrange
        ISignalRClientGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRClientGrain>("TestHub:conn5");
        await grain.ConnectAsync("TestHub", "server1");

        // Act
        await grain.DisconnectAsync();
        await grain.DisconnectAsync();
        string? serverId = await grain.GetServerIdAsync();

        // Assert
        Assert.Null(serverId);
    }

    /// <summary>
    ///     Tests that disconnect clears the connected state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "DisconnectAsync Clears Server Id")]
    public async Task DisconnectAsyncShouldClearServerId()
    {
        // Arrange
        ISignalRClientGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRClientGrain>("TestHub:conn2");
        await grain.ConnectAsync("TestHub", "server1");

        // Act
        await grain.DisconnectAsync();
        string? serverId = await grain.GetServerIdAsync();

        // Assert
        Assert.Null(serverId);
    }

    /// <summary>
    ///     Tests that GetServerIdAsync returns null for new grain.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "GetServerIdAsync Returns Null For New Grain")]
    public async Task GetServerIdAsyncShouldReturnNullForNewGrain()
    {
        // Arrange
        ISignalRClientGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRClientGrain>("TestHub:new-conn");

        // Act
        string? serverId = await grain.GetServerIdAsync();

        // Assert
        Assert.Null(serverId);
    }

    /// <summary>
    ///     Tests that SendMessageAsync does not throw when not connected.
    ///     Note: When connected, testing SendMessageAsync requires full stream provider
    ///     configuration which is beyond the scope of L0 unit tests.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendMessageAsync Does Not Throw When Not Connected")]
    public async Task SendMessageAsyncShouldNotThrowWhenNotConnected()
    {
        // Arrange
        ISignalRClientGrain grain =
            TestClusterAccess.Cluster.Client.GetGrain<ISignalRClientGrain>("TestHub:not-connected");

        // Act - not connected, so the grain should skip sending without error
        Exception? exception = await Record.ExceptionAsync(() => grain.SendMessageAsync(
            "TestMethod",
            ImmutableArray.Create<object?>("arg1", "arg2")));

        // Assert
        Assert.Null(exception);
    }
}