using System;


namespace Mississippi.EventSourcing.Aggregates.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateKey" /> behavior.
/// </summary>
public class AggregateKeyTests
{
    /// <summary>
    ///     Constructor should succeed with valid entity ID.
    /// </summary>
    [Fact]
    public void ConstructorSucceedsWithValidEntityId()
    {
        AggregateKey key = new("entity-1");
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     Constructor should throw ArgumentException when entityId exceeds max length.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdExceedsMaxLength()
    {
        string longEntityId = new('a', 4193);
        Assert.Throws<ArgumentException>(() => new AggregateKey(longEntityId));
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateKey(null!));
    }

    /// <summary>
    ///     Equality should work correctly for identical keys.
    /// </summary>
    [Fact]
    public void EqualityWorksForIdenticalKeys()
    {
        AggregateKey key1 = new("entity-1");
        AggregateKey key2 = new("entity-1");
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
    }

    /// <summary>
    ///     FromString should parse valid entity ID string.
    /// </summary>
    [Fact]
    public void FromStringParsesValidEntityIdString()
    {
        AggregateKey key = AggregateKey.FromString("entity-1");
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     FromString should throw ArgumentNullException when value is null.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenValueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => AggregateKey.FromString(null!));
    }

    /// <summary>
    ///     GetHashCode should return same value for equal keys.
    /// </summary>
    [Fact]
    public void GetHashCodeReturnsSameValueForEqualKeys()
    {
        AggregateKey key1 = new("entity-1");
        AggregateKey key2 = new("entity-1");
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    ///     Implicit conversion from string should work correctly.
    /// </summary>
    [Fact]
    public void ImplicitConversionFromStringWorks()
    {
        AggregateKey key = "entity-1";
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     Implicit conversion to string should return entity ID.
    /// </summary>
    [Fact]
    public void ImplicitConversionToStringReturnsEntityId()
    {
        AggregateKey key = new("entity-1");
        string stringValue = key;
        Assert.Equal("entity-1", stringValue);
    }

    /// <summary>
    ///     Inequality should work correctly for different keys.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentKeys()
    {
        AggregateKey key1 = new("entity-1");
        AggregateKey key2 = new("entity-2");
        Assert.NotEqual(key1, key2);
        Assert.True(key1 != key2);
    }

    /// <summary>
    ///     Parse should create key from string.
    /// </summary>
    [Fact]
    public void ParseCreatesKeyFromString()
    {
        AggregateKey key = AggregateKey.Parse("entity-1");
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     ToString should return entity ID.
    /// </summary>
    [Fact]
    public void ToStringReturnsEntityId()
    {
        AggregateKey key = new("entity-1");
        Assert.Equal("entity-1", key.ToString());
    }
}