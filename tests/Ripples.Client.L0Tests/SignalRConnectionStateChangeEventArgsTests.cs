using System;

using Allure.Xunit.Attributes;


namespace Mississippi.Ripples.Client.L0Tests;

/// <summary>
///     Tests for <see cref="SignalRConnectionStateChangeEventArgs" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples")]
[AllureSuite("Client")]
[AllureSubSuite("SignalRConnectionStateChangeEventArgs")]
public sealed class SignalRConnectionStateChangeEventArgsTests
{
    /// <summary>
    ///     All connection states can be used.
    /// </summary>
    /// <param name="state">The connection state to test.</param>
    [Theory]
    [AllureFeature("Event Args")]
    [InlineData(SignalRConnectionState.Disconnected)]
    [InlineData(SignalRConnectionState.Connecting)]
    [InlineData(SignalRConnectionState.Connected)]
    [InlineData(SignalRConnectionState.Reconnecting)]
    public void AllConnectionStatesCanBeUsed(
        SignalRConnectionState state
    )
    {
        // Arrange & Act
        SignalRConnectionStateChangeEventArgs sut = new(state, SignalRConnectionState.Disconnected);

        // Assert
        sut.PreviousState.Should().Be(state);
    }

    /// <summary>
    ///     Constructor sets exception when provided.
    /// </summary>
    [Fact]
    [AllureFeature("Event Args")]
    public void ConstructorSetsExceptionWhenProvided()
    {
        // Arrange
        SignalRConnectionState previousState = SignalRConnectionState.Connected;
        SignalRConnectionState currentState = SignalRConnectionState.Disconnected;
        InvalidOperationException exception = new("Connection failed");

        // Act
        SignalRConnectionStateChangeEventArgs sut = new(previousState, currentState, exception);

        // Assert
        sut.PreviousState.Should().Be(previousState);
        sut.CurrentState.Should().Be(currentState);
        sut.Exception.Should().Be(exception);
    }

    /// <summary>
    ///     Constructor sets properties correctly without exception.
    /// </summary>
    [Fact]
    [AllureFeature("Event Args")]
    public void ConstructorSetsPropertiesWithoutException()
    {
        // Arrange
        SignalRConnectionState previousState = SignalRConnectionState.Disconnected;
        SignalRConnectionState currentState = SignalRConnectionState.Connected;

        // Act
        SignalRConnectionStateChangeEventArgs sut = new(previousState, currentState);

        // Assert
        sut.PreviousState.Should().Be(previousState);
        sut.CurrentState.Should().Be(currentState);
        sut.Exception.Should().BeNull();
    }

    /// <summary>
    ///     EventArgs inherits from System.EventArgs.
    /// </summary>
    [Fact]
    [AllureFeature("Event Args")]
    public void InheritsFromSystemEventArgs()
    {
        // Arrange & Act
        SignalRConnectionStateChangeEventArgs sut = new(
            SignalRConnectionState.Disconnected,
            SignalRConnectionState.Connected);

        // Assert
        sut.Should().BeAssignableTo<EventArgs>();
    }
}