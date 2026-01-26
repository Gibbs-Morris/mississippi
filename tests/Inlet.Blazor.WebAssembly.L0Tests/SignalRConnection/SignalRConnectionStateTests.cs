using Allure.Xunit.Attributes;

using Mississippi.Inlet.Client.SignalRConnection;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.SignalRConnection;

/// <summary>
///     Tests for <see cref="SignalRConnectionState" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("SignalRConnection")]
[AllureSubSuite("SignalRConnectionState")]
public sealed class SignalRConnectionStateTests
{
    /// <summary>
    ///     Verifies that default state has Disconnected status.
    /// </summary>
    [Fact]
    [AllureFeature("DefaultState")]
    public void DefaultStateHasDisconnectedStatus()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Equal(SignalRConnectionStatus.Disconnected, state.Status);
    }

    /// <summary>
    ///     Verifies that default state has null values for optional properties.
    /// </summary>
    [Fact]
    [AllureFeature("DefaultState")]
    public void DefaultStateHasNullOptionalProperties()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Null(state.ConnectionId);
        Assert.Null(state.LastError);
        Assert.Null(state.LastConnectedAt);
        Assert.Null(state.LastDisconnectedAt);
        Assert.Null(state.LastMessageReceivedAt);
    }

    /// <summary>
    ///     Verifies that default state has zero reconnect attempts.
    /// </summary>
    [Fact]
    [AllureFeature("DefaultState")]
    public void DefaultStateHasZeroReconnectAttempts()
    {
        // Act
        SignalRConnectionState state = new();

        // Assert
        Assert.Equal(0, state.ReconnectAttemptCount);
    }

    /// <summary>
    ///     Verifies that the feature key is correct.
    /// </summary>
    [Fact]
    [AllureFeature("FeatureKey")]
    public void FeatureKeyReturnsExpectedValue()
    {
        // Act
        string featureKey = SignalRConnectionState.FeatureKey;

        // Assert
        Assert.Equal("signalr-connection", featureKey);
    }

    /// <summary>
    ///     Verifies that state is immutable via with expressions.
    /// </summary>
    [Fact]
    [AllureFeature("Immutability")]
    public void StateIsImmutableViaWithExpressions()
    {
        // Arrange
        SignalRConnectionState original = new()
        {
            Status = SignalRConnectionStatus.Disconnected,
        };

        // Act
        SignalRConnectionState modified = original with
        {
            Status = SignalRConnectionStatus.Connected,
        };

        // Assert
        Assert.Equal(SignalRConnectionStatus.Disconnected, original.Status);
        Assert.Equal(SignalRConnectionStatus.Connected, modified.Status);
        Assert.NotSame(original, modified);
    }
}