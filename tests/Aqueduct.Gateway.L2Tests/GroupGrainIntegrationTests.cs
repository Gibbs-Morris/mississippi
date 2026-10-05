using Mississippi.Aqueduct.Abstractions.Grains;


namespace Mississippi.Aqueduct.Gateway.L2Tests;

/// <summary>
///     Integration tests for SignalR group grain behavior.
///     Note: Full streaming tests are not included due to:
///     1. Serialization issues with collection expressions in Orleans streams
///     2. Key format mismatch between group grain (connectionId) and client grain (hubName:connectionId)
///     These issues should be addressed in production code.
/// </summary>
[Collection(AqueductTests.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class GroupGrainIntegrationTests
#pragma warning restore CA1515
{
    private readonly AqueductFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GroupGrainIntegrationTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aqueduct fixture.</param>
    public GroupGrainIntegrationTests(
        AqueductFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Verifies that adding a connection to a group succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task AddConnectionSucceeds()
    {
        // Arrange
        string hubName = "TestHub";
        string groupName = Guid.NewGuid().ToString("N");
        string connectionId = Guid.NewGuid().ToString("N");
        ISignalRGroupGrain groupGrain = fixture.GetGroupGrain(hubName, groupName);

        // Act
        Func<Task> act = () => groupGrain.AddConnectionAsync(connectionId);

        // Assert
        Assert.Null(await Record.ExceptionAsync(act));
    }

    /// <summary>
    ///     Verifies that duplicate adds don't create duplicate connections.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DuplicateAddDoesNotCreateDuplicates()
    {
        // Arrange
        string hubName = "TestHub";
        string groupName = Guid.NewGuid().ToString("N");
        string connectionId = Guid.NewGuid().ToString("N");
        ISignalRGroupGrain groupGrain = fixture.GetGroupGrain(hubName, groupName);

        // Add the same connection multiple times
        await groupGrain.AddConnectionAsync(connectionId);
        await groupGrain.AddConnectionAsync(connectionId);
        await groupGrain.AddConnectionAsync(connectionId);

        // Act
        ImmutableHashSet<string> connections = await groupGrain.GetConnectionsAsync();

        // Assert - should only have one entry
        Assert.Single(connections);
        Assert.Contains(connectionId, connections);
    }

    /// <summary>
    ///     Verifies that an empty group returns empty connections.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task EmptyGroupReturnsNoConnections()
    {
        // Arrange
        string hubName = "TestHub";
        string groupName = Guid.NewGuid().ToString("N");
        ISignalRGroupGrain groupGrain = fixture.GetGroupGrain(hubName, groupName);

        // Act
        ImmutableHashSet<string> connections = await groupGrain.GetConnectionsAsync();

        // Assert
        Assert.Empty(connections);
    }

    /// <summary>
    ///     Verifies that added connections appear in the group.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetConnectionsReturnsAddedConnections()
    {
        // Arrange
        string hubName = "TestHub";
        string groupName = Guid.NewGuid().ToString("N");
        string connectionId1 = Guid.NewGuid().ToString("N");
        string connectionId2 = Guid.NewGuid().ToString("N");
        ISignalRGroupGrain groupGrain = fixture.GetGroupGrain(hubName, groupName);
        await groupGrain.AddConnectionAsync(connectionId1);
        await groupGrain.AddConnectionAsync(connectionId2);

        // Act
        ImmutableHashSet<string> connections = await groupGrain.GetConnectionsAsync();

        // Assert
        Assert.Contains(connectionId1, connections);
        Assert.Contains(connectionId2, connections);
        Assert.Equal(2, connections.Count);
    }

    /// <summary>
    ///     Verifies that removing a connection removes it from the group.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RemoveConnectionRemovesFromGroup()
    {
        // Arrange
        string hubName = "TestHub";
        string groupName = Guid.NewGuid().ToString("N");
        string connectionId1 = Guid.NewGuid().ToString("N");
        string connectionId2 = Guid.NewGuid().ToString("N");
        ISignalRGroupGrain groupGrain = fixture.GetGroupGrain(hubName, groupName);
        await groupGrain.AddConnectionAsync(connectionId1);
        await groupGrain.AddConnectionAsync(connectionId2);
        await groupGrain.RemoveConnectionAsync(connectionId1);

        // Act
        ImmutableHashSet<string> connections = await groupGrain.GetConnectionsAsync();

        // Assert
        Assert.DoesNotContain(connectionId1, connections);
        Assert.Contains(connectionId2, connections);
        Assert.Single(connections);
    }

    /// <summary>
    ///     Verifies that removing a non-existent connection doesn't throw.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RemoveNonExistentConnectionDoesNotThrow()
    {
        // Arrange
        string hubName = "TestHub";
        string groupName = Guid.NewGuid().ToString("N");
        string connectionId = Guid.NewGuid().ToString("N");
        ISignalRGroupGrain groupGrain = fixture.GetGroupGrain(hubName, groupName);

        // Act - remove connection that was never added
        Func<Task> act = () => groupGrain.RemoveConnectionAsync(connectionId);

        // Assert
        Assert.Null(await Record.ExceptionAsync(act));
    }

    /// <summary>
    ///     Verifies that sending a message to an empty group does not throw.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SendMessageToEmptyGroupDoesNotThrow()
    {
        // Arrange
        string hubName = "TestHub";
        string groupName = Guid.NewGuid().ToString("N");
        ISignalRGroupGrain groupGrain = fixture.GetGroupGrain(hubName, groupName);

        // Act
        Func<Task> act = () => groupGrain.SendMessageAsync("Method", []);

        // Assert
        Assert.Null(await Record.ExceptionAsync(act));
    }
}