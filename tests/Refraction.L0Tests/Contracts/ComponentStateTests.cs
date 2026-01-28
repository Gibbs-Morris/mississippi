using System;


using Mississippi.Refraction.Contracts;


namespace Mississippi.Refraction.L0Tests.Contracts;

/// <summary>
///     Tests for <see cref="ComponentState" /> record struct.
/// </summary>
public sealed class ComponentStateTests
{
    /// <summary>
    ///     ComponentState Active converts to correct string.
    /// </summary>
    [Fact]
        public void ComponentStateActiveConvertsToCorrectString()
    {
        // Arrange
        ComponentState state = ComponentState.Active;

        // Act
        string result = state;

        // Assert
        Assert.Equal(RefractionStates.Active, result);
    }

    /// <summary>
    ///     ComponentState Busy converts to correct string.
    /// </summary>
    [Fact]
        public void ComponentStateBusyConvertsToCorrectString()
    {
        // Arrange
        ComponentState state = ComponentState.Busy;

        // Act
        string result = state;

        // Assert
        Assert.Equal(RefractionStates.Busy, result);
    }

    /// <summary>
    ///     ComponentState Custom creates custom state.
    /// </summary>
    [Fact]
        public void ComponentStateCustomCreatesCustomState()
    {
        // Arrange & Act
        ComponentState state = ComponentState.Custom("enterprise-override");

        // Assert
        string result = state;
        Assert.Equal("enterprise-override", result);
    }

    /// <summary>
    ///     ComponentState Disabled converts to correct string.
    /// </summary>
    [Fact]
        public void ComponentStateDisabledConvertsToCorrectString()
    {
        // Arrange
        ComponentState state = ComponentState.Disabled;

        // Act
        string result = state;

        // Assert
        Assert.Equal(RefractionStates.Disabled, result);
    }

    /// <summary>
    ///     ComponentState Error converts to correct string.
    /// </summary>
    [Fact]
        public void ComponentStateErrorConvertsToCorrectString()
    {
        // Arrange
        ComponentState state = ComponentState.Error;

        // Act
        string result = state;

        // Assert
        Assert.Equal(RefractionStates.Error, result);
    }

    /// <summary>
    ///     ComponentState Expanded converts to correct string.
    /// </summary>
    [Fact]
        public void ComponentStateExpandedConvertsToCorrectString()
    {
        // Arrange
        ComponentState state = ComponentState.Expanded;

        // Act
        string result = state;

        // Assert
        Assert.Equal(RefractionStates.Expanded, result);
    }

    /// <summary>
    ///     ComponentState Focused converts to correct string.
    /// </summary>
    [Fact]
        public void ComponentStateFocusedConvertsToCorrectString()
    {
        // Arrange
        ComponentState state = ComponentState.Focused;

        // Act
        string result = state;

        // Assert
        Assert.Equal(RefractionStates.Focused, result);
    }

    /// <summary>
    ///     ComponentState Idle converts to correct string.
    /// </summary>
    [Fact]
        public void ComponentStateIdleConvertsToCorrectString()
    {
        // Arrange
        ComponentState state = ComponentState.Idle;

        // Act
        string result = state;

        // Assert
        Assert.Equal(RefractionStates.Idle, result);
    }

    /// <summary>
    ///     ComponentState implements implicit string conversion.
    /// </summary>
    [Fact]
        public void ComponentStateImplementsImplicitStringConversion()
    {
        // Arrange
        ComponentState state = ComponentState.Active;

        // Act - implicit conversion
        string stringValue = state;

        // Assert
        Assert.NotNull(stringValue);
        Assert.IsType<string>(stringValue);
    }

    /// <summary>
    ///     ComponentState is readonly record struct.
    /// </summary>
    [Fact]
        public void ComponentStateIsReadonlyRecordStruct()
    {
        // Arrange
        Type stateType = typeof(ComponentState);

        // Assert
        Assert.True(stateType.IsValueType);
    }

    /// <summary>
    ///     ComponentState Latent converts to correct string.
    /// </summary>
    [Fact]
        public void ComponentStateLatentConvertsToCorrectString()
    {
        // Arrange
        ComponentState state = ComponentState.Latent;

        // Act
        string result = state;

        // Assert
        Assert.Equal(RefractionStates.Latent, result);
    }

    /// <summary>
    ///     ComponentState Locked converts to correct string.
    /// </summary>
    [Fact]
        public void ComponentStateLockedConvertsToCorrectString()
    {
        // Arrange
        ComponentState state = ComponentState.Locked;

        // Act
        string result = state;

        // Assert
        Assert.Equal(RefractionStates.Locked, result);
    }

    /// <summary>
    ///     ComponentState ToString returns correct value.
    /// </summary>
    [Fact]
        public void ComponentStateToStringReturnsCorrectValue()
    {
        // Arrange
        ComponentState state = ComponentState.Error;

        // Act
        string result = state.ToString();

        // Assert
        Assert.Equal(RefractionStates.Error, result);
    }
}