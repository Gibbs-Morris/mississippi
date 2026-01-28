using System;


using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionCursorKey" /> behavior.
/// </summary>
public sealed class UxProjectionCursorKeyTests
{
    /// <summary>
    ///     Constructor should create key with valid values.
    /// </summary>
    [Fact]
    public void ConstructorCreatesValidKey()
    {
        UxProjectionCursorKey key = new("TestBrook", "entity-1");
        Assert.Equal("TestBrook", key.BrookName);
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     Constructor should throw when brook name contains separator.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookNameContainsSeparator()
    {
        Assert.Throws<ArgumentException>(() => new UxProjectionCursorKey("Test|Brook", "entity-1"));
    }

    /// <summary>
    ///     Constructor should throw when brook name is empty.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new UxProjectionCursorKey(string.Empty, "entity-1"));
    }

    /// <summary>
    ///     Constructor should throw when brook name is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new UxProjectionCursorKey(null!, "entity-1"));
    }

    /// <summary>
    ///     Constructor should throw when entity ID contains separator.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdContainsSeparator()
    {
        Assert.Throws<ArgumentException>(() => new UxProjectionCursorKey("TestBrook", "entity|1"));
    }

    /// <summary>
    ///     Constructor should throw when entity ID is empty.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new UxProjectionCursorKey("TestBrook", string.Empty));
    }

    /// <summary>
    ///     Constructor should throw when entity ID is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new UxProjectionCursorKey("TestBrook", null!));
    }

    /// <summary>
    ///     Constructor should throw when composite key exceeds max length.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenKeyExceedsMaxLength()
    {
        string longName = new('x', 2097); // 2097 + 2097 + 1 separator > 4192
        Assert.Throws<ArgumentException>(() => new UxProjectionCursorKey(longName, longName));
    }

    /// <summary>
    ///     Equality comparison should fail for different keys.
    /// </summary>
    [Fact]
    public void EqualityFailsForDifferentKeys()
    {
        UxProjectionCursorKey key1 = new("TestBrook", "entity-1");
        UxProjectionCursorKey key2 = new("TestBrook", "entity-2");
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
        UxProjectionCursorKey key1 = new("TestBrook", "entity-1");
        UxProjectionCursorKey key2 = new("TestBrook", "entity-1");
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    /// <summary>
    ///     FromBrookKey should create key from brook key.
    /// </summary>
    [Fact]
    public void FromBrookKeyCreatesValidKey()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        UxProjectionCursorKey key = UxProjectionCursorKey.FromBrookKey(brookKey);
        Assert.Equal("TestBrook", key.BrookName);
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     GetHashCode should be consistent for equal keys.
    /// </summary>
    [Fact]
    public void GetHashCodeIsConsistentForEqualKeys()
    {
        UxProjectionCursorKey key1 = new("TestBrook", "entity-1");
        UxProjectionCursorKey key2 = new("TestBrook", "entity-1");
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    ///     Implicit conversion to string should work.
    /// </summary>
    [Fact]
    public void ImplicitConversionToStringWorks()
    {
        UxProjectionCursorKey key = new("TestBrook", "entity-1");
        string result = key;
        Assert.Equal("TestBrook|entity-1", result);
    }

    /// <summary>
    ///     Parse should create key from valid string.
    /// </summary>
    [Fact]
    public void ParseCreatesKeyFromValidString()
    {
        UxProjectionCursorKey key = UxProjectionCursorKey.Parse("TestBrook|entity-1");
        Assert.Equal("TestBrook", key.BrookName);
        Assert.Equal("entity-1", key.EntityId);
    }

    /// <summary>
    ///     Parse should throw when string format is invalid.
    /// </summary>
    [Fact]
    public void ParseThrowsWhenFormatIsInvalid()
    {
        Assert.Throws<FormatException>(() => UxProjectionCursorKey.Parse("invalid-format"));
    }

    /// <summary>
    ///     Parse should throw when string is null.
    /// </summary>
    [Fact]
    public void ParseThrowsWhenStringIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => UxProjectionCursorKey.Parse(null!));
    }

    /// <summary>
    ///     Parse should throw when string has too many parts.
    /// </summary>
    [Fact]
    public void ParseThrowsWhenTooManyParts()
    {
        Assert.Throws<FormatException>(() => UxProjectionCursorKey.Parse("one|two|three"));
    }

    /// <summary>
    ///     ToBrookKey should return correct BrookKey.
    /// </summary>
    [Fact]
    public void ToBrookKeyReturnsCorrectBrookKey()
    {
        UxProjectionCursorKey key = new("TestBrook", "entity-1");
        BrookKey brookKey = key.ToBrookKey();
        Assert.Equal("TestBrook", brookKey.BrookName);
        Assert.Equal("entity-1", brookKey.EntityId);
    }

    /// <summary>
    ///     ToString should return string representation.
    /// </summary>
    [Fact]
    public void ToStringReturnsStringRepresentation()
    {
        UxProjectionCursorKey key = new("TestBrook", "entity-1");
        Assert.Equal("TestBrook|entity-1", key.ToString());
    }

    /// <summary>
    ///     TryParse should fail for empty string.
    /// </summary>
    [Fact]
    public void TryParseFailsForEmptyString()
    {
        bool result = UxProjectionCursorKey.TryParse(string.Empty, out UxProjectionCursorKey key);
        Assert.False(result);
        Assert.Equal(default, key);
    }

    /// <summary>
    ///     TryParse should fail for invalid format.
    /// </summary>
    [Fact]
    public void TryParseFailsForInvalidFormat()
    {
        bool result = UxProjectionCursorKey.TryParse("invalid-format", out UxProjectionCursorKey key);
        Assert.False(result);
        Assert.Equal(default, key);
    }

    /// <summary>
    ///     TryParse should fail for null string.
    /// </summary>
    [Fact]
    public void TryParseFailsForNullString()
    {
        bool result = UxProjectionCursorKey.TryParse(null, out UxProjectionCursorKey key);
        Assert.False(result);
        Assert.Equal(default, key);
    }

    /// <summary>
    ///     TryParse should fail when component contains separator.
    /// </summary>
    [Fact]
    public void TryParseFailsWhenComponentContainsSeparator()
    {
        // After parse, validation should fail for whitespace-only parts
        bool result = UxProjectionCursorKey.TryParse("|entity-1", out UxProjectionCursorKey key);
        Assert.False(result);
        Assert.Equal(default, key);
    }

    /// <summary>
    ///     TryParse should succeed for valid string.
    /// </summary>
    [Fact]
    public void TryParseSucceedsForValidString()
    {
        bool result = UxProjectionCursorKey.TryParse("TestBrook|entity-1", out UxProjectionCursorKey key);
        Assert.True(result);
        Assert.Equal("TestBrook", key.BrookName);
        Assert.Equal("entity-1", key.EntityId);
    }
}