using System;
using System.Reflection;

using Mississippi.Tributary.Abstractions.Attributes;


namespace Mississippi.Tributary.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotRetentionAttribute" />.
/// </summary>
public sealed class SnapshotRetentionAttributeTests
{
    /// <summary>
    ///     Test state decorated with a retention modulus.
    /// </summary>
    [SnapshotRetention(20)]
    private sealed class DecoratedState
    {
    }

    /// <summary>
    ///     Verifies that the attribute applies only once to classes and is not inherited.
    /// </summary>
    [Fact]
    public void AttributeUsageMatchesRetentionContract()
    {
        AttributeUsageAttribute? usage =
            typeof(SnapshotRetentionAttribute).GetCustomAttribute<AttributeUsageAttribute>();
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
        Assert.Equal(20, typeof(DecoratedState).GetCustomAttribute<SnapshotRetentionAttribute>()?.Modulus);
    }

    /// <summary>
    ///     Verifies that nonpositive modulus values are rejected.
    /// </summary>
    /// <param name="modulus">The invalid modulus.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsNonpositiveModulus(
        int modulus
    )
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnapshotRetentionAttribute(modulus));
    }

    /// <summary>
    ///     Verifies that the constructor stores the configured modulus.
    /// </summary>
    [Fact]
    public void ConstructorStoresModulus()
    {
        SnapshotRetentionAttribute attribute = new(20);
        Assert.Equal(20, attribute.Modulus);
    }
}