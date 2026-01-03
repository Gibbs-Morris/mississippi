using Allure.Xunit.Attributes;


namespace Mississippi.Ripples.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="RipplePoolStats" /> record.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Abstractions")]
[AllureSubSuite("RipplePoolStats")]
public sealed class RipplePoolStatsTests
{
    /// <summary>
    ///     Verifies that RipplePoolStats stores CacheHits correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Record Properties")]
    public void CacheHitsPropertyReturnsConstructorValue()
    {
        // Arrange & Act
        RipplePoolStats stats = new(1, 2, 3, 4);

        // Assert
        Assert.Equal(4, stats.CacheHits);
    }

    /// <summary>
    ///     Verifies that RipplePoolStats.Empty has all zero values.
    /// </summary>
    [Fact]
    [AllureFeature("Static Members")]
    public void EmptyHasAllZeroValues()
    {
        // Arrange & Act
        RipplePoolStats empty = RipplePoolStats.Empty;

        // Assert
        Assert.Equal(0, empty.HotCount);
        Assert.Equal(0, empty.WarmCount);
        Assert.Equal(0, empty.TotalFetches);
        Assert.Equal(0, empty.CacheHits);
    }

    /// <summary>
    ///     Verifies that RipplePoolStats.Empty returns the same instance.
    /// </summary>
    [Fact]
    [AllureFeature("Static Members")]
    public void EmptyReturnsSameInstance()
    {
        // Arrange & Act
        RipplePoolStats empty1 = RipplePoolStats.Empty;
        RipplePoolStats empty2 = RipplePoolStats.Empty;

        // Assert
        Assert.Same(empty1, empty2);
    }

    /// <summary>
    ///     Verifies that RipplePoolStats stores HotCount correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Record Properties")]
    public void HotCountPropertyReturnsConstructorValue()
    {
        // Arrange & Act
        RipplePoolStats stats = new(1, 2, 3, 4);

        // Assert
        Assert.Equal(1, stats.HotCount);
    }

    /// <summary>
    ///     Verifies that RipplePoolStats stores TotalFetches correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Record Properties")]
    public void TotalFetchesPropertyReturnsConstructorValue()
    {
        // Arrange & Act
        RipplePoolStats stats = new(1, 2, 3, 4);

        // Assert
        Assert.Equal(3, stats.TotalFetches);
    }

    /// <summary>
    ///     Verifies that RipplePoolStats supports value equality.
    /// </summary>
    [Fact]
    [AllureFeature("Value Equality")]
    public void ValueEqualityWorksCorrectly()
    {
        // Arrange
        RipplePoolStats stats1 = new(1, 2, 3, 4);
        RipplePoolStats stats2 = new(1, 2, 3, 4);
        RipplePoolStats stats3 = new(0, 0, 0, 0);

        // Assert
        Assert.Equal(stats1, stats2);
        Assert.NotEqual(stats1, stats3);
    }

    /// <summary>
    ///     Verifies that RipplePoolStats stores WarmCount correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Record Properties")]
    public void WarmCountPropertyReturnsConstructorValue()
    {
        // Arrange & Act
        RipplePoolStats stats = new(1, 2, 3, 4);

        // Assert
        Assert.Equal(2, stats.WarmCount);
    }
}