using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.SignalR.Grains;

using Moq;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.SignalR.L0Tests.Grains;

/// <summary>
///     Unit tests for <see cref="UxGroupGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections SignalR")]
[AllureSubSuite("UxGroupGrain")]
public sealed class UxGroupGrainTests
{
    private const string ConnectionId1 = "connection-abc123";

    private const string ConnectionId2 = "connection-def456";

    private static UxGroupGrain CreateGrain(
        string grainKey
    )
    {
        Mock<IGrainContext> context = new();
        Mock<IGrainFactory> grainFactory = new();
        Mock<ILogger<UxGroupGrain>> logger = new();
        GrainId grainId = GrainId.Create("ux-group", grainKey);
        context.SetupGet(c => c.GrainId).Returns(grainId);
        return new(context.Object, grainFactory.Object, logger.Object);
    }

    private const string GroupName = "chat-room-1";

    private const string HubName = "TestHub";

    /// <summary>
    ///     AddConnectionAsync should add connection to the group.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AddConnectionAsyncAddsConnectionToGroup()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");

        // Act
        await grain.AddConnectionAsync(ConnectionId1);

        // Assert
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();
        Assert.Single(connections);
        Assert.Contains(ConnectionId1, connections);
    }

    /// <summary>
    ///     AddConnectionAsync should handle duplicate connections gracefully.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AddConnectionAsyncHandlesDuplicates()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");
        await grain.AddConnectionAsync(ConnectionId1);

        // Act - add same connection again
        await grain.AddConnectionAsync(ConnectionId1);

        // Assert - should still have only one connection
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();
        Assert.Single(connections);
    }

    /// <summary>
    ///     AddConnectionAsync should support multiple connections.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AddConnectionAsyncSupportsMultipleConnections()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");

        // Act
        await grain.AddConnectionAsync(ConnectionId1);
        await grain.AddConnectionAsync(ConnectionId2);

        // Assert
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();
        Assert.Equal(2, connections.Count);
        Assert.Contains(ConnectionId1, connections);
        Assert.Contains(ConnectionId2, connections);
    }

    /// <summary>
    ///     AddConnectionAsync should throw when connectionId is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AddConnectionAsyncThrowsWhenConnectionIdIsNull()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.AddConnectionAsync(null!));
    }

    /// <summary>
    ///     Constructor should throw when grainContext is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        Mock<IGrainFactory> grainFactory = new();
        Mock<ILogger<UxGroupGrain>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxGroupGrain(null!, grainFactory.Object, logger.Object));
    }

    /// <summary>
    ///     Constructor should throw when grainFactory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainFactoryIsNull()
    {
        // Arrange
        Mock<IGrainContext> context = new();
        Mock<ILogger<UxGroupGrain>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxGroupGrain(context.Object, null!, logger.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainContext> context = new();
        Mock<IGrainFactory> grainFactory = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxGroupGrain(context.Object, grainFactory.Object, null!));
    }

    /// <summary>
    ///     GetConnectionsAsync returns empty set when no connections.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConnectionsAsyncReturnsEmptyWhenNoConnections()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");

        // Act
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();

        // Assert
        Assert.Empty(connections);
    }

    /// <summary>
    ///     OnActivateAsync should complete successfully.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnActivateAsyncCompletes()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");

        // Act
        Func<Task> actionAsync = () => grain.OnActivateAsync(CancellationToken.None);

        // Assert
        Exception? exception = await Record.ExceptionAsync(actionAsync);
        Assert.Null(exception);
    }

    /// <summary>
    ///     RemoveConnectionAsync should handle non-existent connections gracefully.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RemoveConnectionAsyncHandlesNonExistentConnection()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");

        // Act - should not throw
        await grain.RemoveConnectionAsync("non-existent-connection");

        // Assert
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();
        Assert.Empty(connections);
    }

    /// <summary>
    ///     RemoveConnectionAsync should remove connection from the group.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RemoveConnectionAsyncRemovesConnectionFromGroup()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");
        await grain.AddConnectionAsync(ConnectionId1);
        await grain.AddConnectionAsync(ConnectionId2);

        // Act
        await grain.RemoveConnectionAsync(ConnectionId1);

        // Assert
        ImmutableHashSet<string> connections = await grain.GetConnectionsAsync();
        Assert.Single(connections);
        Assert.DoesNotContain(ConnectionId1, connections);
        Assert.Contains(ConnectionId2, connections);
    }

    /// <summary>
    ///     RemoveConnectionAsync should throw when connectionId is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RemoveConnectionAsyncThrowsWhenConnectionIdIsNull()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.RemoveConnectionAsync(null!));
    }

    /// <summary>
    ///     SendAllAsync should throw when args is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAllAsyncThrowsWhenArgsIsNull()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.SendAllAsync("TestMethod", null!));
    }

    /// <summary>
    ///     SendAllAsync should throw when methodName is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendAllAsyncThrowsWhenMethodNameIsNull()
    {
        // Arrange
        UxGroupGrain grain = CreateGrain($"{HubName}:{GroupName}");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.SendAllAsync(null!, []));
    }
}