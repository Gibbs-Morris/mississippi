using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.SignalRConnection;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for SignalR connection actions.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("SignalR Connection")]
[AllureSubSuite("Actions")]
public sealed class SignalRConnectionActionTests
{
    /// <summary>
    ///     SignalRConnectedAction allows null connection ID.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRConnectedActionAllowsNullConnectionId()
    {
        // Arrange
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SignalRConnectedAction action = new(null, timestamp);

        // Assert
        Assert.Null(action.ConnectionId);
    }

    /// <summary>
    ///     SignalRConnectedAction constructor sets properties correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRConnectedActionConstructorSetsPropertiesCorrectly()
    {
        // Arrange
        string connectionId = "conn-123";
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        SignalRConnectedAction action = new(connectionId, timestamp);

        // Assert
        Assert.Equal(connectionId, action.ConnectionId);
        Assert.Equal(timestamp, action.Timestamp);
    }

    /// <summary>
    ///     SignalRConnectingAction constructor creates instance.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRConnectingActionConstructorCreatesInstance()
    {
        // Act
        SignalRConnectingAction action = new();

        // Assert
        Assert.NotNull(action);
    }

    /// <summary>
    ///     SignalRDisconnectedAction allows null error.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRDisconnectedActionAllowsNullError()
    {
        // Arrange
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SignalRDisconnectedAction action = new(null, timestamp);

        // Assert
        Assert.Null(action.Error);
    }

    /// <summary>
    ///     SignalRDisconnectedAction constructor sets properties correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRDisconnectedActionConstructorSetsPropertiesCorrectly()
    {
        // Arrange
        string error = "Connection lost";
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        SignalRDisconnectedAction action = new(error, timestamp);

        // Assert
        Assert.Equal(error, action.Error);
        Assert.Equal(timestamp, action.Timestamp);
    }

    /// <summary>
    ///     SignalRMessageReceivedAction constructor sets property correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRMessageReceivedActionConstructorSetsPropertyCorrectly()
    {
        // Arrange
        DateTimeOffset timestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        SignalRMessageReceivedAction action = new(timestamp);

        // Assert
        Assert.Equal(timestamp, action.Timestamp);
    }

    /// <summary>
    ///     SignalRReconnectedAction allows null connection ID.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRReconnectedActionAllowsNullConnectionId()
    {
        // Arrange
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SignalRReconnectedAction action = new(null, timestamp);

        // Assert
        Assert.Null(action.ConnectionId);
    }

    /// <summary>
    ///     SignalRReconnectedAction constructor sets properties correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRReconnectedActionConstructorSetsPropertiesCorrectly()
    {
        // Arrange
        string connectionId = "conn-456";
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SignalRReconnectedAction action = new(connectionId, timestamp);

        // Assert
        Assert.Equal(connectionId, action.ConnectionId);
        Assert.Equal(timestamp, action.Timestamp);
    }

    /// <summary>
    ///     SignalRReconnectingAction allows null error.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRReconnectingActionAllowsNullError()
    {
        // Arrange
        int attemptNumber = 1;

        // Act
        SignalRReconnectingAction action = new(null, attemptNumber);

        // Assert
        Assert.Null(action.Error);
    }

    /// <summary>
    ///     SignalRReconnectingAction constructor sets properties correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void SignalRReconnectingActionConstructorSetsPropertiesCorrectly()
    {
        // Arrange
        string error = "Network error";
        int attemptNumber = 3;

        // Act
        SignalRReconnectingAction action = new(error, attemptNumber);

        // Assert
        Assert.Equal(error, action.Error);
        Assert.Equal(attemptNumber, action.AttemptNumber);
    }
}