using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="ISignalRGroupGrain" /> operations.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("SignalR Orleans")]
[AllureSubSuite("Group Grain")]
[Collection(ClusterTestSuite.Name)]
public sealed class SignalRGroupGrainTests
{
    /// <summary>
    ///     Tests that a connection can be added to a group.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "AddConnectionAsync Adds Connection")]
    public async Task AddConnectionAsyncShouldAddConnection()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group1");

        // Act
        await grain.AddConnectionAsync("conn1");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Contains("conn1", connections);
    }

    /// <summary>
    ///     Tests that adding the same connection twice is idempotent.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "AddConnectionAsync Is Idempotent")]
    public async Task AddConnectionAsyncShouldBeIdempotent()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group2");

        // Act
        await grain.AddConnectionAsync("conn-idem");
        await grain.AddConnectionAsync("conn-idem");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Single(connections);
        Assert.Contains("conn-idem", connections);
    }

    /// <summary>
    ///     Tests that multiple connections can be added to a group.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "AddConnectionAsync Adds Multiple Connections")]
    public async Task AddConnectionAsyncShouldAddMultipleConnections()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group3");

        // Act
        await grain.AddConnectionAsync("conn-a");
        await grain.AddConnectionAsync("conn-b");
        await grain.AddConnectionAsync("conn-c");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Equal(3, connections.Count);
        Assert.Contains("conn-a", connections);
        Assert.Contains("conn-b", connections);
        Assert.Contains("conn-c", connections);
    }

    /// <summary>
    ///     Tests that a connection can be removed from a group.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "RemoveConnectionAsync Removes Connection")]
    public async Task RemoveConnectionAsyncShouldRemoveConnection()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group4");
        await grain.AddConnectionAsync("conn-remove");

        // Act
        await grain.RemoveConnectionAsync("conn-remove");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.DoesNotContain("conn-remove", connections);
    }

    /// <summary>
    ///     Tests that removing a non-existent connection is a no-op.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "RemoveConnectionAsync Is No-Op For Non-Existent Connection")]
    public async Task RemoveConnectionAsyncShouldBeNoOpForNonExistentConnection()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group5");
        await grain.AddConnectionAsync("conn-keep");

        // Act - removing non-existent connection
        await grain.RemoveConnectionAsync("conn-nonexistent");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert - original connection should still be there
        Assert.Contains("conn-keep", connections);
    }

    /// <summary>
    ///     Tests that GetConnectionsAsync returns empty set for new group.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "GetConnectionsAsync Returns Empty For New Group")]
    public async Task GetConnectionsAsyncShouldReturnEmptyForNewGroup()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:new-group");

        // Act
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Empty(connections);
    }

    /// <summary>
    ///     Tests that SendMessageAsync can be called without throwing for an empty group.
    ///     Note: For groups with connections, the test requires a full stream provider setup
    ///     which is beyond the scope of L0 unit tests.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendMessageAsync Does Not Throw For Empty Group")]
    public async Task SendMessageAsyncShouldNotThrowForEmptyGroup()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:empty-group");

        // Act - Empty group should not invoke any client grains or streams
        Exception? exception = await Record.ExceptionAsync(() => grain.SendMessageAsync("TestMethod", ImmutableArray.Create<object?>("arg1")));

        // Assert
        Assert.Null(exception);
    }
}
