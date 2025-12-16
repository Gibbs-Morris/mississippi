using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.L1Tests.Infrastructure;
using Mississippi.AspNetCore.Orleans.SignalR;
using Mississippi.AspNetCore.Orleans.SignalR.Grains;
using Mississippi.AspNetCore.Orleans.SignalR.Options;
using Moq;
using Orleans;
using Orleans.TestingHost;
using Xunit;

namespace Mississippi.AspNetCore.Orleans.L1Tests.SignalR;

/// <summary>
/// Comprehensive L1 integration tests for <see cref="OrleansHubLifetimeManager{THub}"/> using Orleans TestCluster.
/// </summary>
[Collection(ClusterTestSuite.Name)]
public sealed class OrleansHubLifetimeManagerTests
{
    private readonly TestCluster cluster;

    public OrleansHubLifetimeManagerTests(ClusterFixture fixture)
    {
        cluster = fixture.Cluster;
    }

    /// <summary>
    /// Creates a new OrleansHubLifetimeManager instance for testing with specified options.
    /// </summary>
    private OrleansHubLifetimeManager<TestHub> CreateManager(SignalROptions? options = null)
    {
        var opts = options ?? new SignalROptions { StreamProviderName = "SignalRStreamProvider" };
        var logger = cluster.ServiceProvider.GetService(typeof(ILogger<OrleansHubLifetimeManager<TestHub>>)) as ILogger<OrleansHubLifetimeManager<TestHub>>;
        return new OrleansHubLifetimeManager<TestHub>(
            logger ?? throw new InvalidOperationException("Logger not found"),
            cluster.Client,
            Options.Create(opts));
    }

    /// <summary>
    /// Creates a mock HubConnectionContext for testing.
    /// </summary>
    private Mock<HubConnectionContext> CreateMockConnection(string connectionId, string? userId = null)
    {
        var mock = new Mock<HubConnectionContext>();
        mock.Setup(c => c.ConnectionId).Returns(connectionId);
        
        if (userId != null)
        {
            var userIdentifier = Mock.Of<IUserIdProvider>(p => p.GetUserId(It.IsAny<HubConnectionContext>()) == userId);
            mock.Setup(c => c.UserIdentifier).Returns(userId);
        }

        return mock;
    }

    /// <summary>
    /// OnConnectedAsync registers a connection successfully.
    /// </summary>
    [Fact]
    public async Task OnConnectedAsync_NewConnection_RegistersSuccessfully()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";
        var mockConnection = CreateMockConnection(connectionId);

        // Act & Assert (should not throw)
        await manager.OnConnectedAsync(mockConnection.Object);
    }

    /// <summary>
    /// OnConnectedAsync with null connection should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task OnConnectedAsync_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.OnConnectedAsync(null!));
    }

    /// <summary>
    /// OnDisconnectedAsync unregisters a connection successfully.
    /// </summary>
    [Fact]
    public async Task OnDisconnectedAsync_ExistingConnection_UnregistersSuccessfully()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";
        var mockConnection = CreateMockConnection(connectionId);

        await manager.OnConnectedAsync(mockConnection.Object);

        // Act & Assert (should not throw)
        await manager.OnDisconnectedAsync(mockConnection.Object);
    }

    /// <summary>
    /// OnDisconnectedAsync with null connection should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task OnDisconnectedAsync_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.OnDisconnectedAsync(null!));
    }

    /// <summary>
    /// SendAllAsync sends message to all connected clients.
    /// </summary>
    [Fact]
    public async Task SendAllAsync_WithConnections_SendsToAll()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId1 = $"conn-{Guid.NewGuid()}";
        var connectionId2 = $"conn-{Guid.NewGuid()}";
        var mockConnection1 = CreateMockConnection(connectionId1);
        var mockConnection2 = CreateMockConnection(connectionId2);

        await manager.OnConnectedAsync(mockConnection1.Object);
        await manager.OnConnectedAsync(mockConnection2.Object);

        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };

        // Act & Assert (should not throw)
        await manager.SendAllAsync(methodName, args, CancellationToken.None);
    }

    /// <summary>
    /// SendAllAsync with null method name should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task SendAllAsync_NullMethodName_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();
        var args = new object[] { "test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.SendAllAsync(null!, args, CancellationToken.None));
    }

    /// <summary>
    /// SendAllExceptAsync excludes specified connections.
    /// </summary>
    [Fact]
    public async Task SendAllExceptAsync_WithExcludedConnections_ExcludesCorrectly()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId1 = $"conn-{Guid.NewGuid()}";
        var connectionId2 = $"conn-{Guid.NewGuid()}";
        var mockConnection1 = CreateMockConnection(connectionId1);
        var mockConnection2 = CreateMockConnection(connectionId2);

        await manager.OnConnectedAsync(mockConnection1.Object);
        await manager.OnConnectedAsync(mockConnection2.Object);

        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };
        var excludedConnectionIds = new[] { connectionId1 };

        // Act & Assert (should not throw)
        await manager.SendAllExceptAsync(methodName, args, excludedConnectionIds, CancellationToken.None);
    }

    /// <summary>
    /// SendConnectionAsync sends message to specific connection.
    /// </summary>
    [Fact]
    public async Task SendConnectionAsync_ExistingConnection_SendsSuccessfully()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";
        var mockConnection = CreateMockConnection(connectionId);

        await manager.OnConnectedAsync(mockConnection.Object);

        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };

        // Act & Assert (should not throw)
        await manager.SendConnectionAsync(connectionId, methodName, args, CancellationToken.None);
    }

    /// <summary>
    /// SendConnectionAsync with null connection ID should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task SendConnectionAsync_NullConnectionId_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();
        var methodName = "TestMethod";
        var args = new object[] { "test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.SendConnectionAsync(null!, methodName, args, CancellationToken.None));
    }

    /// <summary>
    /// SendConnectionsAsync sends message to multiple specific connections.
    /// </summary>
    [Fact]
    public async Task SendConnectionsAsync_MultipleConnections_SendsToAll()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId1 = $"conn-{Guid.NewGuid()}";
        var connectionId2 = $"conn-{Guid.NewGuid()}";
        var mockConnection1 = CreateMockConnection(connectionId1);
        var mockConnection2 = CreateMockConnection(connectionId2);

        await manager.OnConnectedAsync(mockConnection1.Object);
        await manager.OnConnectedAsync(mockConnection2.Object);

        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };
        var connectionIds = new[] { connectionId1, connectionId2 };

        // Act & Assert (should not throw)
        await manager.SendConnectionsAsync(connectionIds, methodName, args, CancellationToken.None);
    }

    /// <summary>
    /// SendGroupAsync sends message to all connections in a group.
    /// </summary>
    [Fact]
    public async Task SendGroupAsync_WithGroupMembers_SendsToGroup()
    {
        // Arrange
        var manager = CreateManager();
        var groupName = $"group-{Guid.NewGuid()}";
        var connectionId1 = $"conn-{Guid.NewGuid()}";
        var connectionId2 = $"conn-{Guid.NewGuid()}";
        var mockConnection1 = CreateMockConnection(connectionId1);
        var mockConnection2 = CreateMockConnection(connectionId2);

        await manager.OnConnectedAsync(mockConnection1.Object);
        await manager.OnConnectedAsync(mockConnection2.Object);
        await manager.AddToGroupAsync(connectionId1, groupName);
        await manager.AddToGroupAsync(connectionId2, groupName);

        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };

        // Act & Assert (should not throw)
        await manager.SendGroupAsync(groupName, methodName, args, CancellationToken.None);
    }

    /// <summary>
    /// SendGroupAsync with null group name should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task SendGroupAsync_NullGroupName_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();
        var methodName = "TestMethod";
        var args = new object[] { "test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.SendGroupAsync(null!, methodName, args, CancellationToken.None));
    }

    /// <summary>
    /// SendGroupExceptAsync excludes specified connections from group send.
    /// </summary>
    [Fact]
    public async Task SendGroupExceptAsync_WithExcludedConnections_ExcludesCorrectly()
    {
        // Arrange
        var manager = CreateManager();
        var groupName = $"group-{Guid.NewGuid()}";
        var connectionId1 = $"conn-{Guid.NewGuid()}";
        var connectionId2 = $"conn-{Guid.NewGuid()}";
        var mockConnection1 = CreateMockConnection(connectionId1);
        var mockConnection2 = CreateMockConnection(connectionId2);

        await manager.OnConnectedAsync(mockConnection1.Object);
        await manager.OnConnectedAsync(mockConnection2.Object);
        await manager.AddToGroupAsync(connectionId1, groupName);
        await manager.AddToGroupAsync(connectionId2, groupName);

        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };
        var excludedConnectionIds = new[] { connectionId1 };

        // Act & Assert (should not throw)
        await manager.SendGroupExceptAsync(groupName, methodName, args, excludedConnectionIds, CancellationToken.None);
    }

    /// <summary>
    /// SendGroupsAsync sends message to multiple groups.
    /// </summary>
    [Fact]
    public async Task SendGroupsAsync_MultipleGroups_SendsToAll()
    {
        // Arrange
        var manager = CreateManager();
        var groupName1 = $"group1-{Guid.NewGuid()}";
        var groupName2 = $"group2-{Guid.NewGuid()}";
        var connectionId1 = $"conn-{Guid.NewGuid()}";
        var connectionId2 = $"conn-{Guid.NewGuid()}";
        var mockConnection1 = CreateMockConnection(connectionId1);
        var mockConnection2 = CreateMockConnection(connectionId2);

        await manager.OnConnectedAsync(mockConnection1.Object);
        await manager.OnConnectedAsync(mockConnection2.Object);
        await manager.AddToGroupAsync(connectionId1, groupName1);
        await manager.AddToGroupAsync(connectionId2, groupName2);

        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };
        var groupNames = new[] { groupName1, groupName2 };

        // Act & Assert (should not throw)
        await manager.SendGroupsAsync(groupNames, methodName, args, CancellationToken.None);
    }

    /// <summary>
    /// SendUserAsync sends message to all connections for a user.
    /// </summary>
    [Fact]
    public async Task SendUserAsync_WithUserConnections_SendsToUser()
    {
        // Arrange
        var manager = CreateManager();
        var userId = $"user-{Guid.NewGuid()}";
        var connectionId1 = $"conn-{Guid.NewGuid()}";
        var connectionId2 = $"conn-{Guid.NewGuid()}";
        var mockConnection1 = CreateMockConnection(connectionId1, userId);
        var mockConnection2 = CreateMockConnection(connectionId2, userId);

        await manager.OnConnectedAsync(mockConnection1.Object);
        await manager.OnConnectedAsync(mockConnection2.Object);

        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };

        // Act & Assert (should not throw)
        await manager.SendUserAsync(userId, methodName, args, CancellationToken.None);
    }

    /// <summary>
    /// SendUserAsync with null user ID should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task SendUserAsync_NullUserId_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();
        var methodName = "TestMethod";
        var args = new object[] { "test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.SendUserAsync(null!, methodName, args, CancellationToken.None));
    }

    /// <summary>
    /// SendUsersAsync sends message to multiple users.
    /// </summary>
    [Fact]
    public async Task SendUsersAsync_MultipleUsers_SendsToAll()
    {
        // Arrange
        var manager = CreateManager();
        var userId1 = $"user1-{Guid.NewGuid()}";
        var userId2 = $"user2-{Guid.NewGuid()}";
        var connectionId1 = $"conn-{Guid.NewGuid()}";
        var connectionId2 = $"conn-{Guid.NewGuid()}";
        var mockConnection1 = CreateMockConnection(connectionId1, userId1);
        var mockConnection2 = CreateMockConnection(connectionId2, userId2);

        await manager.OnConnectedAsync(mockConnection1.Object);
        await manager.OnConnectedAsync(mockConnection2.Object);

        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };
        var userIds = new[] { userId1, userId2 };

        // Act & Assert (should not throw)
        await manager.SendUsersAsync(userIds, methodName, args, CancellationToken.None);
    }

    /// <summary>
    /// AddToGroupAsync adds connection to a group successfully.
    /// </summary>
    [Fact]
    public async Task AddToGroupAsync_NewGroupMember_AddsSuccessfully()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";
        var groupName = $"group-{Guid.NewGuid()}";
        var mockConnection = CreateMockConnection(connectionId);

        await manager.OnConnectedAsync(mockConnection.Object);

        // Act & Assert (should not throw)
        await manager.AddToGroupAsync(connectionId, groupName);
    }

    /// <summary>
    /// AddToGroupAsync with null connection ID should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task AddToGroupAsync_NullConnectionId_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();
        var groupName = "test-group";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.AddToGroupAsync(null!, groupName));
    }

    /// <summary>
    /// AddToGroupAsync with null group name should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task AddToGroupAsync_NullGroupName_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.AddToGroupAsync(connectionId, null!));
    }

    /// <summary>
    /// RemoveFromGroupAsync removes connection from a group successfully.
    /// </summary>
    [Fact]
    public async Task RemoveFromGroupAsync_ExistingGroupMember_RemovesSuccessfully()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";
        var groupName = $"group-{Guid.NewGuid()}";
        var mockConnection = CreateMockConnection(connectionId);

        await manager.OnConnectedAsync(mockConnection.Object);
        await manager.AddToGroupAsync(connectionId, groupName);

        // Act & Assert (should not throw)
        await manager.RemoveFromGroupAsync(connectionId, groupName);
    }

    /// <summary>
    /// RemoveFromGroupAsync with null connection ID should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task RemoveFromGroupAsync_NullConnectionId_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();
        var groupName = "test-group";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.RemoveFromGroupAsync(null!, groupName));
    }

    /// <summary>
    /// RemoveFromGroupAsync with null group name should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task RemoveFromGroupAsync_NullGroupName_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.RemoveFromGroupAsync(connectionId, null!));
    }

    /// <summary>
    /// Multiple connections can be added to the same group.
    /// </summary>
    [Fact]
    public async Task AddToGroupAsync_MultipleConnections_AddedToSameGroup()
    {
        // Arrange
        var manager = CreateManager();
        var groupName = $"group-{Guid.NewGuid()}";
        var connectionId1 = $"conn-{Guid.NewGuid()}";
        var connectionId2 = $"conn-{Guid.NewGuid()}";
        var mockConnection1 = CreateMockConnection(connectionId1);
        var mockConnection2 = CreateMockConnection(connectionId2);

        await manager.OnConnectedAsync(mockConnection1.Object);
        await manager.OnConnectedAsync(mockConnection2.Object);

        // Act & Assert (should not throw)
        await manager.AddToGroupAsync(connectionId1, groupName);
        await manager.AddToGroupAsync(connectionId2, groupName);
    }

    /// <summary>
    /// Connection can be added to multiple groups.
    /// </summary>
    [Fact]
    public async Task AddToGroupAsync_SingleConnection_AddedToMultipleGroups()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";
        var groupName1 = $"group1-{Guid.NewGuid()}";
        var groupName2 = $"group2-{Guid.NewGuid()}";
        var mockConnection = CreateMockConnection(connectionId);

        await manager.OnConnectedAsync(mockConnection.Object);

        // Act & Assert (should not throw)
        await manager.AddToGroupAsync(connectionId, groupName1);
        await manager.AddToGroupAsync(connectionId, groupName2);
    }

    /// <summary>
    /// OnDisconnectedAsync removes connection from all groups.
    /// </summary>
    [Fact]
    public async Task OnDisconnectedAsync_RemovesFromAllGroups()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";
        var groupName = $"group-{Guid.NewGuid()}";
        var mockConnection = CreateMockConnection(connectionId);

        await manager.OnConnectedAsync(mockConnection.Object);
        await manager.AddToGroupAsync(connectionId, groupName);

        // Act
        await manager.OnDisconnectedAsync(mockConnection.Object);

        // Assert - Sending to group should not throw even though connection is gone
        var methodName = "TestMethod";
        var args = new object[] { "test-arg" };
        await manager.SendGroupAsync(groupName, methodName, args, CancellationToken.None);
    }

    /// <summary>
    /// CancellationToken is honored by SendAllAsync.
    /// </summary>
    [Fact]
    public async Task SendAllAsync_CancellationToken_Honored()
    {
        // Arrange
        var manager = CreateManager();
        var methodName = "TestMethod";
        var args = new object[] { "test" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.SendAllAsync(methodName, args, cts.Token));
    }

    /// <summary>
    /// Idempotent operations - adding same connection to same group twice doesn't cause issues.
    /// </summary>
    [Fact]
    public async Task AddToGroupAsync_Idempotent_NoErrorOnDuplicate()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";
        var groupName = $"group-{Guid.NewGuid()}";
        var mockConnection = CreateMockConnection(connectionId);

        await manager.OnConnectedAsync(mockConnection.Object);

        // Act & Assert (should not throw even when adding twice)
        await manager.AddToGroupAsync(connectionId, groupName);
        await manager.AddToGroupAsync(connectionId, groupName);
    }

    /// <summary>
    /// Idempotent operations - removing connection from group twice doesn't cause issues.
    /// </summary>
    [Fact]
    public async Task RemoveFromGroupAsync_Idempotent_NoErrorOnDuplicate()
    {
        // Arrange
        var manager = CreateManager();
        var connectionId = $"conn-{Guid.NewGuid()}";
        var groupName = $"group-{Guid.NewGuid()}";
        var mockConnection = CreateMockConnection(connectionId);

        await manager.OnConnectedAsync(mockConnection.Object);
        await manager.AddToGroupAsync(connectionId, groupName);

        // Act & Assert (should not throw even when removing twice)
        await manager.RemoveFromGroupAsync(connectionId, groupName);
        await manager.RemoveFromGroupAsync(connectionId, groupName);
    }
}
