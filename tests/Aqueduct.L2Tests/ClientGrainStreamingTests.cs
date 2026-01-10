using Mississippi.Aqueduct.Abstractions.Grains;


namespace Mississippi.Aqueduct.L2Tests;

/// <summary>
///     Integration tests for SignalR client grain behavior.
///     Note: Streaming tests are not included because the production code has serialization
///     issues with internal compiler types when ImmutableArray/collection expressions are used
///     with object?[] args. These would need to be addressed in production code first.
/// </summary>
[Collection(AqueductTests.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class ClientGrainStreamingTests
#pragma warning restore CA1515
{
    private readonly AqueductFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ClientGrainStreamingTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aqueduct fixture.</param>
    public ClientGrainStreamingTests(
        AqueductFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Verifies that a connected client grain has the expected server ID.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ConnectedClientHasServerId()
    {
        // Arrange
        string hubName = "TestHub";
        string connectionId = Guid.NewGuid().ToString("N");
        string serverId = Guid.NewGuid().ToString("N");
        ISignalRClientGrain clientGrain = fixture.GetClientGrain(hubName, connectionId);

        // Act
        await clientGrain.ConnectAsync(hubName, serverId);
        string? retrievedServerId = await clientGrain.GetServerIdAsync();

        // Assert
        retrievedServerId.Should().Be(serverId);
    }

    /// <summary>
    ///     Verifies that after disconnect, the server ID is cleared.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DisconnectClearsServerId()
    {
        // Arrange
        string hubName = "TestHub";
        string connectionId = Guid.NewGuid().ToString("N");
        string serverId = Guid.NewGuid().ToString("N");
        ISignalRClientGrain clientGrain = fixture.GetClientGrain(hubName, connectionId);
        await clientGrain.ConnectAsync(hubName, serverId);

        // Act
        await clientGrain.DisconnectAsync();

        // After disconnect, the grain deactivates. Getting a fresh reference
        // should show no connection state
        ISignalRClientGrain freshGrain = fixture.GetClientGrain(hubName, connectionId);
        string? retrievedServerId = await freshGrain.GetServerIdAsync();

        // Assert
        retrievedServerId.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that a disconnected client grain returns null server ID.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DisconnectedClientHasNullServerId()
    {
        // Arrange
        string hubName = "TestHub";
        string connectionId = Guid.NewGuid().ToString("N");
        ISignalRClientGrain clientGrain = fixture.GetClientGrain(hubName, connectionId);

        // Act - grain was never connected
        string? serverId = await clientGrain.GetServerIdAsync();

        // Assert
        serverId.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that multiple connections on same grain key update the state.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconnectUpdatesServerId()
    {
        // Arrange
        string hubName = "TestHub";
        string connectionId = Guid.NewGuid().ToString("N");
        string serverId1 = Guid.NewGuid().ToString("N");
        string serverId2 = Guid.NewGuid().ToString("N");
        ISignalRClientGrain clientGrain = fixture.GetClientGrain(hubName, connectionId);

        // Act - connect to server1, then reconnect to server2
        await clientGrain.ConnectAsync(hubName, serverId1);
        await clientGrain.ConnectAsync(hubName, serverId2);
        string? retrievedServerId = await clientGrain.GetServerIdAsync();

        // Assert - should have the latest server
        retrievedServerId.Should().Be(serverId2);
    }
}