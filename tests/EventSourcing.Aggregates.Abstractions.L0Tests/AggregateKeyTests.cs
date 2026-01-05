using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateKey" /> behavior.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates Abstractions")]
[AllureSubSuite("Aggregate Key")]
public class AggregateKeyTests
{
    /// <summary>
    ///     Constructor should succeed with valid inputs.
    /// </summary>
    [Fact]
    public void ConstructorSucceedsWithValidInputs()
    {
        AggregateKey key = new("TestAggregate", "entity-1");
        Assert.Equal("TestAggregate", key.AggregateTypeName);
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     Constructor should throw ArgumentException when aggregateTypeName contains separator.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenAggregateTypeNameContainsSeparator()
    {
        Assert.Throws<ArgumentException>(() => new AggregateKey("Test|Aggregate", "entity-1"));
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when aggregateTypeName is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenAggregateTypeNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateKey(null!, "entity-1"));
    }

    /// <summary>
    ///     Constructor should throw ArgumentException when composite key exceeds max length.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenCompositeKeyExceedsMaxLength()
    {
        string longTypeName = new('a', 600);
        string longEntityId = new('b', 500);
        Assert.Throws<ArgumentException>(() => new AggregateKey(longTypeName, longEntityId));
    }

    /// <summary>
    ///     Constructor should throw ArgumentException when entityId contains separator.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdContainsSeparator()
    {
        Assert.Throws<ArgumentException>(() => new AggregateKey("TestAggregate", "entity|1"));
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregateKey("TestAggregate", null!));
    }

    /// <summary>
    ///     Equality should work correctly for identical keys.
    /// </summary>
    [Fact]
    public void EqualityWorksForIdenticalKeys()
    {
        AggregateKey key1 = new("TestAggregate", "entity-1");
        AggregateKey key2 = new("TestAggregate", "entity-1");
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
    }

    /// <summary>
    ///     FromString should parse valid key string.
    /// </summary>
    [Fact]
    public void FromStringParsesValidKeyString()
    {
        AggregateKey key = AggregateKey.FromString("TestAggregate|entity-1");
        Assert.Equal("TestAggregate", key.AggregateTypeName);
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     FromString should throw FormatException when separator is missing.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenSeparatorMissing()
    {
        Assert.Throws<FormatException>(() => AggregateKey.FromString("NoSeparatorHere"));
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
        AggregateKey key1 = new("TestAggregate", "entity-1");
        AggregateKey key2 = new("TestAggregate", "entity-1");
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    ///     Implicit conversion from string should parse correctly.
    /// </summary>
    [Fact]
    public void ImplicitConversionFromStringParsesCorrectly()
    {
        AggregateKey key = "TestAggregate|entity-1";
        Assert.Equal("TestAggregate", key.AggregateTypeName);
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     Implicit conversion to BrookKey should work correctly.
    /// </summary>
    [Fact]
    public void ImplicitConversionToBrookKeyWorks()
    {
        AggregateKey aggregateKey = new("TestAggregate", "entity-1");
        BrookKey brookKey = aggregateKey;
        Assert.Equal("TestAggregate", brookKey.BrookName);
        Assert.Equal("entity-1", brookKey.EntityId);
    }

    /// <summary>
    ///     Implicit conversion to string should return correct format.
    /// </summary>
    [Fact]
    public void ImplicitConversionToStringReturnsCorrectFormat()
    {
        AggregateKey key = new("TestAggregate", "entity-1");
        string stringValue = key;
        Assert.Equal("TestAggregate|entity-1", stringValue);
    }

    /// <summary>
    ///     Inequality should work correctly for different keys.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentKeys()
    {
        AggregateKey key1 = new("TestAggregate", "entity-1");
        AggregateKey key2 = new("TestAggregate", "entity-2");
        Assert.NotEqual(key1, key2);
        Assert.True(key1 != key2);
    }

    /// <summary>
    ///     ToBrookKey should return equivalent BrookKey.
    /// </summary>
    [Fact]
    public void ToBrookKeyReturnsEquivalentBrookKey()
    {
        AggregateKey aggregateKey = new("TestAggregate", "entity-1");
        BrookKey brookKey = aggregateKey.ToBrookKey();
        Assert.Equal("TestAggregate", brookKey.BrookName);
        Assert.Equal("entity-1", brookKey.EntityId);
    }

    /// <summary>
    ///     ToString should return string in correct format.
    /// </summary>
    [Fact]
    public void ToStringReturnsCorrectFormat()
    {
        AggregateKey key = new("TestAggregate", "entity-1");
        Assert.Equal("TestAggregate|entity-1", key.ToString());
    }
}