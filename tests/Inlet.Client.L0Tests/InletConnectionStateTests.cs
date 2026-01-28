using Mississippi.Inlet.Client.Abstractions.State;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for InletConnectionState.
/// </summary>
public sealed class InletConnectionStateTests
{
    /// <summary>
    ///     InletConnectionState can be instantiated.
    /// </summary>
    [Fact]
    public void CanBeInstantiated()
    {
        // Act
        InletConnectionState state = new();

        // Assert
        Assert.NotNull(state);
    }

    /// <summary>
    ///     FeatureKey returns expected value.
    /// </summary>
    [Fact]
    public void FeatureKeyReturnsExpectedValue()
    {
        // Act
        string featureKey = InletConnectionState.FeatureKey;

        // Assert
        Assert.Equal("inlet-connection", featureKey);
    }

    /// <summary>
    ///     InletConnectionState is a record type with value equality.
    /// </summary>
    [Fact]
    public void HasValueEquality()
    {
        // Arrange
        InletConnectionState state1 = new();
        InletConnectionState state2 = new();

        // Assert
        Assert.Equal(state1, state2);
    }
}