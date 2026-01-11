using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionKey" /> behavior.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections Abstractions")]
[AllureSubSuite("UxProjectionKey")]
public sealed class UxProjectionKeyTests
{
    /// <summary>
    ///     Constructor should create key with valid entity ID.
    /// </summary>
    [Fact]
    public void ConstructorCreatesValidKey()
    {
        UxProjectionKey key = new("entity-1");
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     Constructor should throw when entity ID exceeds maximum length.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdExceedsMaxLength()
    {
        string longEntityId = new('x', 4193);
        Assert.Throws<ArgumentException>(() => new UxProjectionKey(longEntityId));
    }

    /// <summary>
    ///     Constructor should throw when entity ID is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new UxProjectionKey(null!));
    }

    /// <summary>
    ///     Equality comparison should work for identical keys.
    /// </summary>
    [Fact]
    public void EqualityWorksForIdenticalKeys()
    {
        UxProjectionKey key1 = new("entity-1");
        UxProjectionKey key2 = new("entity-1");
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    /// <summary>
    ///     FromString should parse valid entity ID string.
    /// </summary>
    [Fact]
    public void FromStringParsesValidEntityId()
    {
        UxProjectionKey key = UxProjectionKey.FromString("entity-1");
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     FromString should throw when value is null.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => UxProjectionKey.FromString(null!));
    }

    /// <summary>
    ///     GetHashCode should be consistent for equal keys.
    /// </summary>
    [Fact]
    public void GetHashCodeIsConsistentForEqualKeys()
    {
        UxProjectionKey key1 = new("entity-1");
        UxProjectionKey key2 = new("entity-1");
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    ///     Implicit conversion from string should work correctly.
    /// </summary>
    [Fact]
    public void ImplicitConversionFromStringWorks()
    {
        UxProjectionKey key = "entity-1";
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     Implicit conversion to string should return entity ID.
    /// </summary>
    [Fact]
    public void ImplicitConversionToStringWorks()
    {
        UxProjectionKey key = new("entity-1");
        string result = key;
        Assert.Equal("entity-1", result);
    }

    /// <summary>
    ///     Inequality comparison should work for different keys.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentKeys()
    {
        UxProjectionKey key1 = new("entity-1");
        UxProjectionKey key2 = new("entity-2");
        Assert.NotEqual(key1, key2);
        Assert.False(key1 == key2);
        Assert.True(key1 != key2);
    }

    /// <summary>
    ///     Parse should create key from string.
    /// </summary>
    [Fact]
    public void ParseCreatesKeyFromString()
    {
        UxProjectionKey key = UxProjectionKey.Parse("entity-1");
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     Roundtrip through string and back preserves key.
    /// </summary>
    [Fact]
    public void RoundtripThroughStringPreservesKey()
    {
        UxProjectionKey original = new("entity-1");
        string stringForm = original;
        UxProjectionKey parsed = stringForm;
        Assert.Equal(original, parsed);
    }

    /// <summary>
    ///     ToString should return entity ID.
    /// </summary>
    [Fact]
    public void ToStringReturnsEntityId()
    {
        UxProjectionKey key = new("entity-1");
        Assert.Equal("entity-1", key.ToString());
    }
}