using Xunit;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for <see cref="DevToolsInitializationTracker" />.
/// </summary>
public sealed class DevToolsInitializationTrackerTests
{
    /// <summary>
    ///     WasInitialized should default to false.
    /// </summary>
    [Fact]
    public void WasInitializedDefaultsToFalse()
    {
        // Arrange & Act
        DevToolsInitializationTracker tracker = new();

        // Assert
        Assert.False(tracker.WasInitialized);
    }

    /// <summary>
    ///     WasInitialized can be set to true.
    /// </summary>
    [Fact]
    public void WasInitializedCanBeSetToTrue()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();

        // Act
        tracker.WasInitialized = true;

        // Assert
        Assert.True(tracker.WasInitialized);
    }

    /// <summary>
    ///     WasInitialized can be set back to false.
    /// </summary>
    [Fact]
    public void WasInitializedCanBeSetBackToFalse()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new()
        {
            WasInitialized = true,
        };

        // Act
        tracker.WasInitialized = false;

        // Assert
        Assert.False(tracker.WasInitialized);
    }
}
