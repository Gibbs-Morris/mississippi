// <copyright file="SignalRClientGrainTests.cs" company="Gibbs-Morris">
// Proprietary and Confidential.
// All rights reserved.
// </copyright>

using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.Testing.Utilities.Orleans;
using Mississippi.Viaduct.Grains;
using Mississippi.Viaduct.L0Tests.Infrastructure;


namespace Mississippi.Viaduct.L0Tests;

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
}