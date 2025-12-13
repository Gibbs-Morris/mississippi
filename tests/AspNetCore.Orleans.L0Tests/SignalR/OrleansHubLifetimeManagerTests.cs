namespace Mississippi.AspNetCore.Orleans.L0Tests.SignalR;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.SignalR;
using Mississippi.AspNetCore.Orleans.SignalR.Options;
using Moq;
using Orleans;
using Xunit;

/// <summary>
/// Tests for <see cref="OrleansHubLifetimeManager{THub}"/>.
/// </summary>
public sealed class OrleansHubLifetimeManagerTests
{
    /// <summary>
    /// OnConnectedAsync with null connection should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task OnConnectedAsync_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansHubLifetimeManager<TestHub>>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansSignalROptions());
        var manager = new OrleansHubLifetimeManager<TestHub>(mockLogger.Object, mockCluster.Object, options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.OnConnectedAsync(null!));
    }

    /// <summary>
    /// OnDisconnectedAsync with null connection should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task OnDisconnectedAsync_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansHubLifetimeManager<TestHub>>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansSignalROptions());
        var manager = new OrleansHubLifetimeManager<TestHub>(mockLogger.Object, mockCluster.Object, options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.OnDisconnectedAsync(null!));
    }

    /// <summary>
    /// Test hub for testing purposes.
    /// </summary>
    private sealed class TestHub : Hub
    {
    }
}
