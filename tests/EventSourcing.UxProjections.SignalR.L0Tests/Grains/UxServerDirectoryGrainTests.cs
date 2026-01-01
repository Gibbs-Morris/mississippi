using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.SignalR.Grains;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.SignalR.L0Tests.Grains;

/// <summary>
///     Unit tests for <see cref="UxServerDirectoryGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections SignalR")]
[AllureSubSuite("UxServerDirectoryGrain")]
public sealed class UxServerDirectoryGrainTests
{
    private static UxServerDirectoryGrain CreateGrain()
    {
        Mock<IGrainContext> context = new();
        Mock<ILogger<UxServerDirectoryGrain>> logger = new();
        GrainId grainId = GrainId.Create("ux-server-directory", "default");
        context.SetupGet(c => c.GrainId).Returns(grainId);
        return new(context.Object, logger.Object);
    }

    private const string ServerId1 = "server-001";

    private const string ServerId2 = "server-002";

    /// <summary>
    ///     Constructor should throw when grainContext is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        Mock<ILogger<UxServerDirectoryGrain>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxServerDirectoryGrain(null!, logger.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainContext> context = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxServerDirectoryGrain(context.Object, null!));
    }

    /// <summary>
    ///     GetDeadServersAsync returns empty when no servers are registered.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetDeadServersAsyncReturnsEmptyWhenNoServers()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();

        // Act
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromMinutes(1));

        // Assert
        Assert.Empty(deadServers);
    }

    /// <summary>
    ///     HeartbeatAsync should ignore unknown servers.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HeartbeatAsyncIgnoresUnknownServer()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();

        // Act
        Func<Task> actionAsync = () => grain.HeartbeatAsync("unknown-server", 5);

        // Assert
        Exception? exception = await Record.ExceptionAsync(actionAsync);
        Assert.Null(exception);
    }

    /// <summary>
    ///     HeartbeatAsync should throw when serverId is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HeartbeatAsyncThrowsWhenServerIdIsNull()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.HeartbeatAsync(null!, 5));
    }

    /// <summary>
    ///     HeartbeatAsync should update server's last heartbeat time.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HeartbeatAsyncUpdatesServerHeartbeat()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();
        await grain.RegisterServerAsync(ServerId1);

        // Act - heartbeat with connection count
        await grain.HeartbeatAsync(ServerId1, 10);

        // Assert - no dead servers after heartbeat
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromMinutes(5));
        Assert.Empty(deadServers);
    }

    /// <summary>
    ///     Multiple servers can be registered independently.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MultipleServersCanBeRegistered()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();

        // Act
        await grain.RegisterServerAsync(ServerId1);
        await grain.RegisterServerAsync(ServerId2);
        await grain.HeartbeatAsync(ServerId1, 5);
        await grain.HeartbeatAsync(ServerId2, 10);

        // Assert - both servers are alive
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromMinutes(1));
        Assert.Empty(deadServers);
    }

    /// <summary>
    ///     OnActivateAsync should complete successfully.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnActivateAsyncCompletes()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();

        // Act
        Func<Task> actionAsync = () => grain.OnActivateAsync(CancellationToken.None);

        // Assert
        Exception? exception = await Record.ExceptionAsync(actionAsync);
        Assert.Null(exception);
    }

    /// <summary>
    ///     RegisterServerAsync should add server to the directory.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RegisterServerAsyncAddsServerToDirectory()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();
        await grain.RegisterServerAsync(ServerId1);

        // Act - heartbeat should work for registered server
        await grain.HeartbeatAsync(ServerId1, 5);

        // Assert - no dead servers since we just heartbeated
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromMinutes(1));
        Assert.Empty(deadServers);
    }

    /// <summary>
    ///     RegisterServerAsync should throw when serverId is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RegisterServerAsyncThrowsWhenServerIdIsNull()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.RegisterServerAsync(null!));
    }

    /// <summary>
    ///     UnregisterServerAsync should handle non-existent server gracefully.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UnregisterServerAsyncHandlesNonExistentServer()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();

        // Act
        Func<Task> actionAsync = () => grain.UnregisterServerAsync("non-existent-server");

        // Assert
        Exception? exception = await Record.ExceptionAsync(actionAsync);
        Assert.Null(exception);
    }

    /// <summary>
    ///     UnregisterServerAsync should remove server from the directory.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UnregisterServerAsyncRemovesServerFromDirectory()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();
        await grain.RegisterServerAsync(ServerId1);

        // Act
        await grain.UnregisterServerAsync(ServerId1);

        // Assert - heartbeat for unregistered server should be ignored (doesn't throw)
        await grain.HeartbeatAsync(ServerId1, 5);

        // GetDeadServersAsync returns empty because server is not registered
        ImmutableList<string> deadServers = await grain.GetDeadServersAsync(TimeSpan.FromSeconds(1));
        Assert.Empty(deadServers);
    }

    /// <summary>
    ///     UnregisterServerAsync should throw when serverId is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UnregisterServerAsyncThrowsWhenServerIdIsNull()
    {
        // Arrange
        UxServerDirectoryGrain grain = CreateGrain();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.UnregisterServerAsync(null!));
    }
}