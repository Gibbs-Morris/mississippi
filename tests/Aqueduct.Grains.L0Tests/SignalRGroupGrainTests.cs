using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Grains.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Aqueduct.Grains.L0Tests;

/// <summary>
///     Tests for <see cref="ISignalRGroupGrain" /> operations.
/// </summary>
[AllureParentSuite("Aqueduct")]
[AllureSuite("Grains")]
[AllureSubSuite("SignalRGroupGrain")]
[Collection(ClusterTestSuite.Name)]
public sealed class SignalRGroupGrainTests
{
    /// <summary>
    ///     Tests that AddConnectionAsync adds connection to group.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "AddConnectionAsync Adds Connection To Group")]
    public async Task AddConnectionAsyncShouldAddConnectionToGroup()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group-1");

        // Act
        await grain.AddConnectionAsync("conn-1");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Single(connections);
        Assert.Contains("conn-1", connections);
    }

    /// <summary>
    ///     Tests that AddConnectionAsync is idempotent.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "AddConnectionAsync Is Idempotent")]
    public async Task AddConnectionAsyncShouldBeIdempotent()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group-2");

        // Act
        await grain.AddConnectionAsync("conn-1");
        await grain.AddConnectionAsync("conn-1");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Single(connections);
    }

    /// <summary>
    ///     Tests that GetConnectionsAsync returns empty for new grain.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "GetConnectionsAsync Returns Empty For New Grain")]
    public async Task GetConnectionsAsyncShouldReturnEmptyForNewGrain()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:empty-group");

        // Act
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Empty(connections);
    }

    /// <summary>
    ///     Tests that multiple connections can be added.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "Multiple Connections Can Be Added")]
    public async Task MultipleConnectionsCanBeAdded()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group-5");

        // Act
        await grain.AddConnectionAsync("conn-1");
        await grain.AddConnectionAsync("conn-2");
        await grain.AddConnectionAsync("conn-3");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Equal(3, connections.Count);
    }

    /// <summary>
    ///     Tests that RemoveConnectionAsync is idempotent.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "RemoveConnectionAsync Is Idempotent")]
    public async Task RemoveConnectionAsyncShouldBeIdempotent()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group-4");
        await grain.AddConnectionAsync("conn-1");

        // Act
        await grain.RemoveConnectionAsync("conn-1");
        await grain.RemoveConnectionAsync("conn-1");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Empty(connections);
    }

    /// <summary>
    ///     Tests that RemoveConnectionAsync removes connection from group.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "RemoveConnectionAsync Removes Connection From Group")]
    public async Task RemoveConnectionAsyncShouldRemoveConnectionFromGroup()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:group-3");
        await grain.AddConnectionAsync("conn-1");
        await grain.AddConnectionAsync("conn-2");

        // Act
        await grain.RemoveConnectionAsync("conn-1");
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Single(connections);
        Assert.Contains("conn-2", connections);
    }

    /// <summary>
    ///     Tests that SendMessageAsync does not throw for empty group.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact(DisplayName = "SendMessageAsync Does Not Throw For Empty Group")]
    public async Task SendMessageAsyncShouldNotThrowForEmptyGroup()
    {
        // Arrange
        ISignalRGroupGrain grain = TestClusterAccess.Cluster.Client.GetGrain<ISignalRGroupGrain>("TestHub:empty-send");

        // Act
        Exception? exception = await Record.ExceptionAsync(() => grain.SendMessageAsync(
            "TestMethod",
            ImmutableArray.Create<object?>("arg1")));

        // Assert
        Assert.Null(exception);
    }
}