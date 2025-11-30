using System;


namespace Mississippi.EventSourcing.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="BrookPosition" /> conversion helpers.
/// </summary>
public sealed class BrookPositionTests
{
    /// <summary>
    ///     Construction with a value less than -1 should throw.
    /// </summary>
    [Fact]
    public void ConstructorLessThanMinusOneThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrookPosition(-2));
    }

    /// <summary>
    ///     Default constructed position should be NotSet with value -1 and comparisons should respect ordering.
    /// </summary>
    [Fact]
    public void DefaultAndComparisonSemantics()
    {
        BrookPosition defaultPos = new();
        Assert.True(defaultPos.NotSet);
        Assert.Equal(-1, defaultPos.Value);
        BrookPosition newer = new(10);
        BrookPosition older = new(5);
        Assert.True(newer.IsNewerThan(older));
        Assert.False(older.IsNewerThan(newer));
    }

    /// <summary>
    ///     FromLong/FromInt64 and conversion helpers should preserve value.
    /// </summary>
    [Fact]
    public void FromAndToConversionsPreserveValue()
    {
        BrookPosition pFromLong = BrookPosition.FromLong(15);
        Assert.Equal(15, pFromLong.Value);
        BrookPosition pFromInt64 = BrookPosition.FromInt64(20);
        Assert.Equal(20, pFromInt64.Value);
        long asLong = pFromLong;
        Assert.Equal(15L, asLong);
        Assert.Equal(20L, pFromInt64.ToLong());
        Assert.Equal(20L, pFromInt64.ToInt64());
    }

    /// <summary>
    ///     Implicit conversions to and from long should preserve value.
    /// </summary>
    [Fact]
    public void ImplicitConversionsPreserveValue()
    {
        BrookPosition implicitFrom = 7;
        Assert.Equal(7, implicitFrom.Value);
        long asLong = implicitFrom;
        Assert.Equal(7L, asLong);
    }

    /// <summary>
    ///     Positions with identical values should not treat either side as newer.
    /// </summary>
    [Fact]
    public void IsNewerThanReturnsFalseWhenValuesEqual()
    {
        BrookPosition left = new(42);
        BrookPosition right = new(42);
        Assert.False(left.IsNewerThan(right));
        Assert.False(right.IsNewerThan(left));
    }
}