using System;

using Mississippi.Inlet.Client.SignalRConnection;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for SignalR connection reducers.
/// </summary>
public sealed class SignalRConnectionReducersTests
{
    /// <summary>
    ///     OnConnected clears last error.
    /// </summary>
    [Fact]
    public void OnConnectedClearsLastError()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            LastError = "Previous error",
        };
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRConnectedAction action = new("conn-123", timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(state, action);

        // Assert
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     OnConnected resets reconnect attempt count.
    /// </summary>
    [Fact]
    public void OnConnectedResetsReconnectAttemptCount()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            ReconnectAttemptCount = 5,
        };
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRConnectedAction action = new("conn-123", timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(state, action);

        // Assert
        Assert.Equal(0, result.ReconnectAttemptCount);
    }

    /// <summary>
    ///     OnConnected sets connection ID from action.
    /// </summary>
    [Fact]
    public void OnConnectedSetsConnectionIdFromAction()
    {
        // Arrange
        SignalRConnectionState state = new();
        string connectionId = "conn-456";
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRConnectedAction action = new(connectionId, timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(state, action);

        // Assert
        Assert.Equal(connectionId, result.ConnectionId);
    }

    /// <summary>
    ///     OnConnected sets last connected timestamp.
    /// </summary>
    [Fact]
    public void OnConnectedSetsLastConnectedAt()
    {
        // Arrange
        SignalRConnectionState state = new();
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRConnectedAction action = new("conn-123", timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(state, action);

        // Assert
        Assert.Equal(timestamp, result.LastConnectedAt);
    }

    /// <summary>
    ///     OnConnected sets status to Connected.
    /// </summary>
    [Fact]
    public void OnConnectedSetsStatusToConnected()
    {
        // Arrange
        SignalRConnectionState state = new();
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRConnectedAction action = new("conn-123", timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(state, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connected, result.Status);
    }

    /// <summary>
    ///     OnConnecting clears last error.
    /// </summary>
    [Fact]
    public void OnConnectingClearsLastError()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            LastError = "Previous error",
        };
        SignalRConnectingAction action = new();

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnecting(state, action);

        // Assert
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     OnConnecting sets status to Connecting.
    /// </summary>
    [Fact]
    public void OnConnectingSetsStatusToConnecting()
    {
        // Arrange
        SignalRConnectionState state = new();
        SignalRConnectingAction action = new();

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnecting(state, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connecting, result.Status);
    }

    /// <summary>
    ///     OnDisconnected clears connection ID.
    /// </summary>
    [Fact]
    public void OnDisconnectedClearsConnectionId()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            ConnectionId = "conn-123",
        };
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRDisconnectedAction action = new(null, timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnDisconnected(state, action);

        // Assert
        Assert.Null(result.ConnectionId);
    }

    /// <summary>
    ///     OnDisconnected sets last disconnected timestamp.
    /// </summary>
    [Fact]
    public void OnDisconnectedSetsLastDisconnectedAt()
    {
        // Arrange
        SignalRConnectionState state = new();
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRDisconnectedAction action = new(null, timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnDisconnected(state, action);

        // Assert
        Assert.Equal(timestamp, result.LastDisconnectedAt);
    }

    /// <summary>
    ///     OnDisconnected sets last error from action.
    /// </summary>
    [Fact]
    public void OnDisconnectedSetsLastError()
    {
        // Arrange
        SignalRConnectionState state = new();
        string error = "Connection lost";
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRDisconnectedAction action = new(error, timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnDisconnected(state, action);

        // Assert
        Assert.Equal(error, result.LastError);
    }

    /// <summary>
    ///     OnDisconnected sets status to Disconnected.
    /// </summary>
    [Fact]
    public void OnDisconnectedSetsStatusToDisconnected()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            Status = SignalRConnectionStatus.Connected,
        };
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRDisconnectedAction action = new(null, timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnDisconnected(state, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Disconnected, result.Status);
    }

    /// <summary>
    ///     OnMessageReceived updates last message received timestamp.
    /// </summary>
    [Fact]
    public void OnMessageReceivedUpdatesTimestamp()
    {
        // Arrange
        SignalRConnectionState state = new();
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRMessageReceivedAction action = new(timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnMessageReceived(state, action);

        // Assert
        Assert.Equal(timestamp, result.LastMessageReceivedAt);
    }

    /// <summary>
    ///     OnReconnected clears last error.
    /// </summary>
    [Fact]
    public void OnReconnectedClearsLastError()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            LastError = "Previous error",
        };
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRReconnectedAction action = new("conn-123", timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnected(state, action);

        // Assert
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     OnReconnected resets reconnect attempt count.
    /// </summary>
    [Fact]
    public void OnReconnectedResetsReconnectAttemptCount()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            ReconnectAttemptCount = 3,
        };
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRReconnectedAction action = new("conn-123", timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnected(state, action);

        // Assert
        Assert.Equal(0, result.ReconnectAttemptCount);
    }

    /// <summary>
    ///     OnReconnected sets connection ID from action.
    /// </summary>
    [Fact]
    public void OnReconnectedSetsConnectionId()
    {
        // Arrange
        SignalRConnectionState state = new();
        string connectionId = "conn-new";
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRReconnectedAction action = new(connectionId, timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnected(state, action);

        // Assert
        Assert.Equal(connectionId, result.ConnectionId);
    }

    /// <summary>
    ///     OnReconnected sets status to Connected.
    /// </summary>
    [Fact]
    public void OnReconnectedSetsStatusToConnected()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            Status = SignalRConnectionStatus.Reconnecting,
        };
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        SignalRReconnectedAction action = new("conn-789", timestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnected(state, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connected, result.Status);
    }

    /// <summary>
    ///     OnReconnecting sets last error from action.
    /// </summary>
    [Fact]
    public void OnReconnectingSetsLastError()
    {
        // Arrange
        SignalRConnectionState state = new();
        string error = "Network timeout";
        SignalRReconnectingAction action = new(error, 1);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnecting(state, action);

        // Assert
        Assert.Equal(error, result.LastError);
    }

    /// <summary>
    ///     OnReconnecting sets reconnect attempt count from action.
    /// </summary>
    [Fact]
    public void OnReconnectingSetsReconnectAttemptCount()
    {
        // Arrange
        SignalRConnectionState state = new();
        int attemptNumber = 5;
        SignalRReconnectingAction action = new(null, attemptNumber);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnecting(state, action);

        // Assert
        Assert.Equal(attemptNumber, result.ReconnectAttemptCount);
    }

    /// <summary>
    ///     OnReconnecting sets status to Reconnecting.
    /// </summary>
    [Fact]
    public void OnReconnectingSetsStatusToReconnecting()
    {
        // Arrange
        SignalRConnectionState state = new();
        SignalRReconnectingAction action = new(null, 1);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnecting(state, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Reconnecting, result.Status);
    }
}