using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.SignalRConnection;

/// <summary>
///     Tests for SignalR connection actions.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("SignalRConnection")]
[AllureSubSuite("Actions")]
public sealed class SignalRConnectionActionsTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2025, 1, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     Verifies that SignalRConnectedAction allows null connection ID.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRConnectedAction")]
    public void SignalRConnectedActionAllowsNullConnectionId()
    {
        // Act
        SignalRConnectedAction action = new(null, TestTimestamp);

        // Assert
        Assert.Null(action.ConnectionId);
    }

    /// <summary>
    ///     Verifies that SignalRConnectedAction stores connection ID and timestamp.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRConnectedAction")]
    public void SignalRConnectedActionStoresProperties()
    {
        // Act
        SignalRConnectedAction action = new("connection-123", TestTimestamp);

        // Assert
        Assert.Equal("connection-123", action.ConnectionId);
        Assert.Equal(TestTimestamp, action.Timestamp);
    }

    /// <summary>
    ///     Verifies that SignalRConnectingAction is a valid action record.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRConnectingAction")]
    public void SignalRConnectingActionIsValidAction()
    {
        // Act
        SignalRConnectingAction action = new();

        // Assert
        Assert.NotNull(action);
    }

    /// <summary>
    ///     Verifies that two SignalRConnectingAction instances are equal.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRConnectingAction")]
    public void SignalRConnectingActionInstancesAreEqual()
    {
        // Arrange
        SignalRConnectingAction action1 = new();
        SignalRConnectingAction action2 = new();

        // Assert
        Assert.Equal(action1, action2);
    }

    /// <summary>
    ///     Verifies that SignalRDisconnectedAction allows null error (intentional close).
    /// </summary>
    [Fact]
    [AllureFeature("SignalRDisconnectedAction")]
    public void SignalRDisconnectedActionAllowsNullError()
    {
        // Act
        SignalRDisconnectedAction action = new(null, TestTimestamp);

        // Assert
        Assert.Null(action.Error);
    }

    /// <summary>
    ///     Verifies that SignalRDisconnectedAction stores error and timestamp.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRDisconnectedAction")]
    public void SignalRDisconnectedActionStoresProperties()
    {
        // Act
        SignalRDisconnectedAction action = new("Server closed connection", TestTimestamp);

        // Assert
        Assert.Equal("Server closed connection", action.Error);
        Assert.Equal(TestTimestamp, action.Timestamp);
    }

    /// <summary>
    ///     Verifies that SignalRMessageReceivedAction stores timestamp.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRMessageReceivedAction")]
    public void SignalRMessageReceivedActionStoresTimestamp()
    {
        // Act
        SignalRMessageReceivedAction action = new(TestTimestamp);

        // Assert
        Assert.Equal(TestTimestamp, action.Timestamp);
    }

    /// <summary>
    ///     Verifies that SignalRMessageReceivedAction with different timestamps are not equal.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRMessageReceivedAction")]
    public void SignalRMessageReceivedActionWithDifferentTimestampsAreNotEqual()
    {
        // Arrange
        DateTimeOffset timestamp1 = new(2025, 1, 15, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset timestamp2 = new(2025, 1, 15, 12, 0, 1, TimeSpan.Zero);

        // Act
        SignalRMessageReceivedAction action1 = new(timestamp1);
        SignalRMessageReceivedAction action2 = new(timestamp2);

        // Assert
        Assert.NotEqual(action1, action2);
    }

    /// <summary>
    ///     Verifies that RequestSignalRConnectionAction is a valid action record.
    /// </summary>
    [Fact]
    [AllureFeature("RequestSignalRConnectionAction")]
    public void RequestSignalRConnectionActionIsValidAction()
    {
        // Act
        RequestSignalRConnectionAction action = new();

        // Assert
        Assert.NotNull(action);
    }

    /// <summary>
    ///     Verifies that two RequestSignalRConnectionAction instances are equal.
    /// </summary>
    [Fact]
    [AllureFeature("RequestSignalRConnectionAction")]
    public void RequestSignalRConnectionActionInstancesAreEqual()
    {
        // Arrange
        RequestSignalRConnectionAction action1 = new();
        RequestSignalRConnectionAction action2 = new();

        // Assert
        Assert.Equal(action1, action2);
    }

    /// <summary>
    ///     Verifies that SignalRReconnectedAction stores connection ID and timestamp.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRReconnectedAction")]
    public void SignalRReconnectedActionStoresProperties()
    {
        // Act
        SignalRReconnectedAction action = new("new-connection-456", TestTimestamp);

        // Assert
        Assert.Equal("new-connection-456", action.ConnectionId);
        Assert.Equal(TestTimestamp, action.Timestamp);
    }

    /// <summary>
    ///     Verifies that SignalRReconnectingAction allows null error.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRReconnectingAction")]
    public void SignalRReconnectingActionAllowsNullError()
    {
        // Act
        SignalRReconnectingAction action = new(null, 1);

        // Assert
        Assert.Null(action.Error);
    }

    /// <summary>
    ///     Verifies that SignalRReconnectingAction stores error and attempt number.
    /// </summary>
    [Fact]
    [AllureFeature("SignalRReconnectingAction")]
    public void SignalRReconnectingActionStoresProperties()
    {
        // Act
        SignalRReconnectingAction action = new("Connection lost", 3);

        // Assert
        Assert.Equal("Connection lost", action.Error);
        Assert.Equal(3, action.AttemptNumber);
    }
}