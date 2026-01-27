using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.SignalRConnection;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for SignalR connection reducers.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("SignalR Connection")]
[AllureSubSuite("Reducers")]
public sealed class SignalRConnectionReducersTests
{
    /// <summary>
    ///     OnConnected clears last error.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
    public void OnConnectedClearsLastError()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            LastError = "Previous error",
        };
        SignalRConnectedAction action = new("conn-123", DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(state, action);

        // Assert
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     OnConnected resets reconnect attempt count.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
    public void OnConnectedResetsReconnectAttemptCount()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            ReconnectAttemptCount = 5,
        };
        SignalRConnectedAction action = new("conn-123", DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(state, action);

        // Assert
        Assert.Equal(0, result.ReconnectAttemptCount);
    }

    /// <summary>
    ///     OnConnected sets connection ID from action.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
    public void OnConnectedSetsConnectionIdFromAction()
    {
        // Arrange
        SignalRConnectionState state = new();
        string connectionId = "conn-456";
        SignalRConnectedAction action = new(connectionId, DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(state, action);

        // Assert
        Assert.Equal(connectionId, result.ConnectionId);
    }

    /// <summary>
    ///     OnConnected sets last connected timestamp.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
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
    [AllureFeature("Reducers")]
    public void OnConnectedSetsStatusToConnected()
    {
        // Arrange
        SignalRConnectionState state = new();
        SignalRConnectedAction action = new("conn-123", DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(state, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connected, result.Status);
    }

    /// <summary>
    ///     OnConnecting clears last error.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
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
    [AllureFeature("Reducers")]
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
    [AllureFeature("Reducers")]
    public void OnDisconnectedClearsConnectionId()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            ConnectionId = "conn-123",
        };
        SignalRDisconnectedAction action = new(null, DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnDisconnected(state, action);

        // Assert
        Assert.Null(result.ConnectionId);
    }

    /// <summary>
    ///     OnDisconnected sets last disconnected timestamp.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
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
    [AllureFeature("Reducers")]
    public void OnDisconnectedSetsLastError()
    {
        // Arrange
        SignalRConnectionState state = new();
        string error = "Connection lost";
        SignalRDisconnectedAction action = new(error, DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnDisconnected(state, action);

        // Assert
        Assert.Equal(error, result.LastError);
    }

    /// <summary>
    ///     OnDisconnected sets status to Disconnected.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
    public void OnDisconnectedSetsStatusToDisconnected()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            Status = SignalRConnectionStatus.Connected,
        };
        SignalRDisconnectedAction action = new(null, DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnDisconnected(state, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Disconnected, result.Status);
    }

    /// <summary>
    ///     OnMessageReceived updates last message received timestamp.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
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
    [AllureFeature("Reducers")]
    public void OnReconnectedClearsLastError()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            LastError = "Previous error",
        };
        SignalRReconnectedAction action = new("conn-123", DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnected(state, action);

        // Assert
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     OnReconnected resets reconnect attempt count.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
    public void OnReconnectedResetsReconnectAttemptCount()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            ReconnectAttemptCount = 3,
        };
        SignalRReconnectedAction action = new("conn-123", DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnected(state, action);

        // Assert
        Assert.Equal(0, result.ReconnectAttemptCount);
    }

    /// <summary>
    ///     OnReconnected sets connection ID from action.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
    public void OnReconnectedSetsConnectionId()
    {
        // Arrange
        SignalRConnectionState state = new();
        string connectionId = "conn-new";
        SignalRReconnectedAction action = new(connectionId, DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnected(state, action);

        // Assert
        Assert.Equal(connectionId, result.ConnectionId);
    }

    /// <summary>
    ///     OnReconnected sets status to Connected.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
    public void OnReconnectedSetsStatusToConnected()
    {
        // Arrange
        SignalRConnectionState state = new()
        {
            Status = SignalRConnectionStatus.Reconnecting,
        };
        SignalRReconnectedAction action = new("conn-789", DateTimeOffset.UtcNow);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnected(state, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connected, result.Status);
    }

    /// <summary>
    ///     OnReconnecting sets last error from action.
    /// </summary>
    [Fact]
    [AllureFeature("Reducers")]
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
    [AllureFeature("Reducers")]
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
    [AllureFeature("Reducers")]
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