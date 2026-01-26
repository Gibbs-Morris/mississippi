using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.SignalRConnection;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.SignalRConnection;

/// <summary>
///     Tests for <see cref="SignalRConnectionReducers" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("SignalRConnection")]
[AllureSubSuite("SignalRConnectionReducers")]
public sealed class SignalRConnectionReducersTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2025, 1, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     Verifies that OnConnected handles null connection ID.
    /// </summary>
    [Fact]
    [AllureFeature("OnConnected")]
    public void OnConnectedHandlesNullConnectionId()
    {
        // Arrange
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Connecting,
        };
        SignalRConnectedAction action = new(null, TestTimestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(initialState, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connected, result.Status);
        Assert.Null(result.ConnectionId);
    }

    /// <summary>
    ///     Verifies that OnConnected sets status to Connected and records connection details.
    /// </summary>
    [Fact]
    [AllureFeature("OnConnected")]
    public void OnConnectedSetsStatusToConnected()
    {
        // Arrange
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Connecting,
            ReconnectAttemptCount = 3,
            LastError = "Some error",
        };
        SignalRConnectedAction action = new("connection-123", TestTimestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnected(initialState, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connected, result.Status);
        Assert.Equal("connection-123", result.ConnectionId);
        Assert.Equal(TestTimestamp, result.LastConnectedAt);
        Assert.Equal(0, result.ReconnectAttemptCount);
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     Verifies that OnConnecting sets status to Connecting and clears error.
    /// </summary>
    [Fact]
    [AllureFeature("OnConnecting")]
    public void OnConnectingSetsStatusToConnecting()
    {
        // Arrange
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Disconnected,
            LastError = "Previous error",
        };
        SignalRConnectingAction action = new();

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnConnecting(initialState, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connecting, result.Status);
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     Verifies that OnDisconnected handles intentional disconnection (null error).
    /// </summary>
    [Fact]
    [AllureFeature("OnDisconnected")]
    public void OnDisconnectedHandlesIntentionalDisconnection()
    {
        // Arrange
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Connected,
            ConnectionId = "connection-123",
        };
        SignalRDisconnectedAction action = new(null, TestTimestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnDisconnected(initialState, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Disconnected, result.Status);
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     Verifies that OnDisconnected sets status to Disconnected and records timestamp.
    /// </summary>
    [Fact]
    [AllureFeature("OnDisconnected")]
    public void OnDisconnectedSetsStatusToDisconnected()
    {
        // Arrange
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Connected,
            ConnectionId = "connection-123",
        };
        SignalRDisconnectedAction action = new("Server closed connection", TestTimestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnDisconnected(initialState, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Disconnected, result.Status);
        Assert.Null(result.ConnectionId);
        Assert.Equal(TestTimestamp, result.LastDisconnectedAt);
        Assert.Equal("Server closed connection", result.LastError);
    }

    /// <summary>
    ///     Verifies that OnMessageReceived preserves all other state properties.
    /// </summary>
    [Fact]
    [AllureFeature("OnMessageReceived")]
    public void OnMessageReceivedPreservesOtherProperties()
    {
        // Arrange
        DateTimeOffset previousConnectedAt = TestTimestamp.AddMinutes(-5);
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Connected,
            ConnectionId = "connection-123",
            LastConnectedAt = previousConnectedAt,
            ReconnectAttemptCount = 0,
            LastError = null,
        };
        SignalRMessageReceivedAction action = new(TestTimestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnMessageReceived(initialState, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connected, result.Status);
        Assert.Equal("connection-123", result.ConnectionId);
        Assert.Equal(previousConnectedAt, result.LastConnectedAt);
        Assert.Equal(0, result.ReconnectAttemptCount);
        Assert.Null(result.LastError);
        Assert.Equal(TestTimestamp, result.LastMessageReceivedAt);
    }

    /// <summary>
    ///     Verifies that OnMessageReceived updates the last message timestamp.
    /// </summary>
    [Fact]
    [AllureFeature("OnMessageReceived")]
    public void OnMessageReceivedUpdatesLastMessageTimestamp()
    {
        // Arrange
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Connected,
            ConnectionId = "connection-123",
        };
        SignalRMessageReceivedAction action = new(TestTimestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnMessageReceived(initialState, action);

        // Assert
        Assert.Equal(TestTimestamp, result.LastMessageReceivedAt);

        // Other properties should remain unchanged
        Assert.Equal(SignalRConnectionStatus.Connected, result.Status);
        Assert.Equal("connection-123", result.ConnectionId);
    }

    /// <summary>
    ///     Verifies that OnReconnected sets status to Connected and resets attempt count.
    /// </summary>
    [Fact]
    [AllureFeature("OnReconnected")]
    public void OnReconnectedSetsStatusToConnected()
    {
        // Arrange
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Reconnecting,
            ConnectionId = "old-connection",
            ReconnectAttemptCount = 3,
            LastError = "Reconnect error",
        };
        SignalRReconnectedAction action = new("new-connection-456", TestTimestamp);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnected(initialState, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connected, result.Status);
        Assert.Equal("new-connection-456", result.ConnectionId);
        Assert.Equal(TestTimestamp, result.LastConnectedAt);
        Assert.Equal(0, result.ReconnectAttemptCount);
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     Verifies that OnReconnecting handles null error.
    /// </summary>
    [Fact]
    [AllureFeature("OnReconnecting")]
    public void OnReconnectingHandlesNullError()
    {
        // Arrange
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Connected,
        };
        SignalRReconnectingAction action = new(null, 1);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnecting(initialState, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Reconnecting, result.Status);
        Assert.Equal(1, result.ReconnectAttemptCount);
        Assert.Null(result.LastError);
    }

    /// <summary>
    ///     Verifies that OnReconnecting sets status and records attempt number.
    /// </summary>
    [Fact]
    [AllureFeature("OnReconnecting")]
    public void OnReconnectingSetsStatusAndAttemptNumber()
    {
        // Arrange
        SignalRConnectionState initialState = new()
        {
            Status = SignalRConnectionStatus.Connected,
            ConnectionId = "old-connection",
        };
        SignalRReconnectingAction action = new("Connection lost", 2);

        // Act
        SignalRConnectionState result = SignalRConnectionReducers.OnReconnecting(initialState, action);

        // Assert
        Assert.Equal(SignalRConnectionStatus.Reconnecting, result.Status);
        Assert.Equal(2, result.ReconnectAttemptCount);
        Assert.Equal("Connection lost", result.LastError);

        // ConnectionId should be preserved during reconnection
        Assert.Equal("old-connection", result.ConnectionId);
    }
}