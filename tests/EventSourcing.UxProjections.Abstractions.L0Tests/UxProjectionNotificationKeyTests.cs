using System;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionNotificationKey" /> behavior.
/// </summary>
public sealed class UxProjectionNotificationKeyTests
{
    /// <summary>
    ///     Constructor should create key with valid values.
    /// </summary>
    [Fact]
    public void ConstructorCreatesValidKey()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        UxProjectionNotificationKey key = new("TestProjection", brookKey);
        Assert.Equal("TestProjection", key.ProjectionTypeName);
        Assert.Equal("TestBrook", key.BrookKey.BrookName);
        Assert.Equal("entity-1", key.BrookKey.EntityId);
        Assert.Equal("entity-1", key.EntityId);
    }

    // Note: BrookKey validates separator in brook name and entity ID internally,
    // so those cases are tested by BrookKey tests, not here.

    /// <summary>
    ///     Constructor should throw when composite key exceeds max length.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenKeyExceedsMaxLength()
    {
        string longName = new('x', 1400); // 1400*3 + 2 separators > 4192
        BrookKey brookKey = new(longName, longName);
        Assert.Throws<ArgumentException>(() => new UxProjectionNotificationKey(longName, brookKey));
    }

    /// <summary>
    ///     Constructor should throw when projection type name contains separator.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenProjectionTypeNameContainsSeparator()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        Assert.Throws<ArgumentException>(() => new UxProjectionNotificationKey("Test|Projection", brookKey));
    }

    /// <summary>
    ///     Constructor should throw when projection type name is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenProjectionTypeNameIsNull()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        Assert.Throws<ArgumentNullException>(() => new UxProjectionNotificationKey(null!, brookKey));
    }

    /// <summary>
    ///     Equality comparison should fail for different keys.
    /// </summary>
    [Fact]
    public void EqualityFailsForDifferentKeys()
    {
        BrookKey brookKey1 = new("TestBrook", "entity-1");
        BrookKey brookKey2 = new("TestBrook", "entity-2");
        UxProjectionNotificationKey key1 = new("TestProjection", brookKey1);
        UxProjectionNotificationKey key2 = new("TestProjection", brookKey2);
        Assert.NotEqual(key1, key2);
        Assert.True(key1 != key2);
        Assert.False(key1 == key2);
    }

    /// <summary>
    ///     Equality comparison should work for identical keys.
    /// </summary>
    [Fact]
    public void EqualityWorksForIdenticalKeys()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        UxProjectionNotificationKey key1 = new("TestProjection", brookKey);
        UxProjectionNotificationKey key2 = new("TestProjection", brookKey);
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    /// <summary>
    ///     FromString should parse valid string.
    /// </summary>
    [Fact]
    public void FromStringParsesValidString()
    {
        UxProjectionNotificationKey key = UxProjectionNotificationKey.FromString("TestProjection|TestBrook|entity-1");
        Assert.Equal("TestProjection", key.ProjectionTypeName);
        Assert.Equal("TestBrook", key.BrookKey.BrookName);
        Assert.Equal("entity-1", key.BrookKey.EntityId);
    }

    /// <summary>
    ///     FromString should throw when format has no separator.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenNoSeparator()
    {
        Assert.Throws<FormatException>(() => UxProjectionNotificationKey.FromString("invalid"));
    }

    /// <summary>
    ///     FromString should throw when string is null.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => UxProjectionNotificationKey.FromString(null!));
    }

    /// <summary>
    ///     FromString should throw when format has only one separator.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenOnlyOneSeparator()
    {
        Assert.Throws<FormatException>(() => UxProjectionNotificationKey.FromString("projection|brook"));
    }

    /// <summary>
    ///     FromUxProjectionNotificationKey should return string representation.
    /// </summary>
    [Fact]
    public void FromUxProjectionNotificationKeyReturnsString()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        UxProjectionNotificationKey key = new("TestProjection", brookKey);
        string result = UxProjectionNotificationKey.FromUxProjectionNotificationKey(key);
        Assert.Equal("TestProjection|TestBrook|entity-1", result);
    }

    /// <summary>
    ///     GetHashCode should be consistent for equal keys.
    /// </summary>
    [Fact]
    public void GetHashCodeIsConsistentForEqualKeys()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        UxProjectionNotificationKey key1 = new("TestProjection", brookKey);
        UxProjectionNotificationKey key2 = new("TestProjection", brookKey);
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    ///     Implicit conversion from string should work.
    /// </summary>
    [Fact]
    public void ImplicitConversionFromStringWorks()
    {
        UxProjectionNotificationKey key = "TestProjection|TestBrook|entity-1";
        Assert.Equal("TestProjection", key.ProjectionTypeName);
        Assert.Equal("TestBrook", key.BrookKey.BrookName);
        Assert.Equal("entity-1", key.BrookKey.EntityId);
    }

    /// <summary>
    ///     Implicit conversion to string should work.
    /// </summary>
    [Fact]
    public void ImplicitConversionToStringWorks()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        UxProjectionNotificationKey key = new("TestProjection", brookKey);
        string result = key;
        Assert.Equal("TestProjection|TestBrook|entity-1", result);
    }

    /// <summary>
    ///     ToString should return string representation.
    /// </summary>
    [Fact]
    public void ToStringReturnsStringRepresentation()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        UxProjectionNotificationKey key = new("TestProjection", brookKey);
        Assert.Equal("TestProjection|TestBrook|entity-1", key.ToString());
    }
}