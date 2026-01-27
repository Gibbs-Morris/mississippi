using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.SignalRConnection;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for SignalR connection state.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("SignalR Connection")]
[AllureSubSuite("State")]
public sealed class SignalRConnectionStateTests
{
    /// <summary>
    ///     ConnectionId can be set via init.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void ConnectionIdCanBeSet()
    {
        // Arrange
        string connectionId = "conn-123";

        // Act
        SignalRConnectionState state = new()
        {
            ConnectionId = connectionId,
        };

        // Assert
        Assert.Equal(connectionId, state.ConnectionId);
    }

    /// <summary>
    ///     ConnectionId is null by default.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void ConnectionIdIsNullByDefault()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Null(state.ConnectionId);
    }

    /// <summary>
    ///     Default status is Disconnected.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void DefaultStatusIsDisconnected()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Equal(SignalRConnectionStatus.Disconnected, state.Status);
    }

    /// <summary>
    ///     FeatureKey returns expected value.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void FeatureKeyReturnsExpectedValue()
    {
        // Act
        string featureKey = SignalRConnectionState.FeatureKey;

        // Assert
        Assert.Equal("signalr-connection", featureKey);
    }

    /// <summary>
    ///     LastConnectedAt can be set via init.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void LastConnectedAtCanBeSet()
    {
        // Arrange
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        SignalRConnectionState state = new()
        {
            LastConnectedAt = timestamp,
        };

        // Assert
        Assert.Equal(timestamp, state.LastConnectedAt);
    }

    /// <summary>
    ///     LastConnectedAt is null by default.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void LastConnectedAtIsNullByDefault()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Null(state.LastConnectedAt);
    }

    /// <summary>
    ///     LastDisconnectedAt is null by default.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void LastDisconnectedAtIsNullByDefault()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Null(state.LastDisconnectedAt);
    }

    /// <summary>
    ///     LastError is null by default.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void LastErrorIsNullByDefault()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Null(state.LastError);
    }

    /// <summary>
    ///     LastMessageReceivedAt is null by default.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void LastMessageReceivedAtIsNullByDefault()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Null(state.LastMessageReceivedAt);
    }

    /// <summary>
    ///     ReconnectAttemptCount can be set via init.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void ReconnectAttemptCountCanBeSet()
    {
        // Arrange
        int count = 5;

        // Act
        SignalRConnectionState state = new()
        {
            ReconnectAttemptCount = count,
        };

        // Assert
        Assert.Equal(count, state.ReconnectAttemptCount);
    }

    /// <summary>
    ///     ReconnectAttemptCount is zero by default.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void ReconnectAttemptCountIsZeroByDefault()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Equal(0, state.ReconnectAttemptCount);
    }

    /// <summary>
    ///     Status can be set via init.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void StatusCanBeSet()
    {
        // Act
        SignalRConnectionState state = new()
        {
            Status = SignalRConnectionStatus.Connected,
        };

        // Assert
        Assert.Equal(SignalRConnectionStatus.Connected, state.Status);
    }
}