using System;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionVersionedCacheKey" /> behavior.
/// </summary>
public sealed class UxProjectionVersionedCacheKeyTests
{
    /// <summary>
    ///     Constructor should create key with valid values.
    /// </summary>
    [Fact]
    public void ConstructorCreatesValidKey()
    {
        BrookPosition version = new(42);
        UxProjectionVersionedCacheKey key = new("TestBrook", "entity-1", version);
        Assert.Equal("TestBrook", key.BrookName);
        Assert.Equal("entity-1", key.EntityId);
        Assert.Equal(42, key.Version.Value);
    }

    /// <summary>
    ///     Constructor should throw when brook name contains separator.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookNameContainsSeparator()
    {
        BrookPosition version = new(42);
        Assert.Throws<ArgumentException>(() => new UxProjectionVersionedCacheKey("Test|Brook", "entity-1", version));
    }

    /// <summary>
    ///     Constructor should throw when brook name is empty.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookNameIsEmpty()
    {
        BrookPosition version = new(42);
        Assert.Throws<ArgumentException>(() => new UxProjectionVersionedCacheKey(string.Empty, "entity-1", version));
    }

    /// <summary>
    ///     Constructor should throw when brook name is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenBrookNameIsNull()
    {
        BrookPosition version = new(42);
        Assert.Throws<ArgumentNullException>(() => new UxProjectionVersionedCacheKey(null!, "entity-1", version));
    }

    /// <summary>
    ///     Constructor should throw when entity ID contains separator.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdContainsSeparator()
    {
        BrookPosition version = new(42);
        Assert.Throws<ArgumentException>(() => new UxProjectionVersionedCacheKey("TestBrook", "entity|1", version));
    }

    /// <summary>
    ///     Constructor should throw when entity ID is empty.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdIsEmpty()
    {
        BrookPosition version = new(42);
        Assert.Throws<ArgumentException>(() => new UxProjectionVersionedCacheKey("TestBrook", string.Empty, version));
    }

    /// <summary>
    ///     Constructor should throw when entity ID is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEntityIdIsNull()
    {
        BrookPosition version = new(42);
        Assert.Throws<ArgumentNullException>(() => new UxProjectionVersionedCacheKey("TestBrook", null!, version));
    }

    /// <summary>
    ///     Constructor should throw when composite key exceeds max length.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenKeyExceedsMaxLength()
    {
        string longName = new('x', 2097); // 2097 + 2097 + version + 2 separators > 4192
        BrookPosition version = new(42);
        Assert.Throws<ArgumentException>(() => new UxProjectionVersionedCacheKey(longName, longName, version));
    }

    /// <summary>
    ///     Constructor should throw when version is not set.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenVersionIsNotSet()
    {
        BrookPosition version = new(); // Default is -1 (NotSet)
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UxProjectionVersionedCacheKey("TestBrook", "entity-1", version));
    }

    /// <summary>
    ///     Equality comparison should fail for different versions.
    /// </summary>
    [Fact]
    public void EqualityFailsForDifferentVersions()
    {
        UxProjectionVersionedCacheKey key1 = new("TestBrook", "entity-1", new(42));
        UxProjectionVersionedCacheKey key2 = new("TestBrook", "entity-1", new(43));
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
        BrookPosition version = new(42);
        UxProjectionVersionedCacheKey key1 = new("TestBrook", "entity-1", version);
        UxProjectionVersionedCacheKey key2 = new("TestBrook", "entity-1", version);
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    /// <summary>
    ///     FromBrookKey should create key from brook key and version.
    /// </summary>
    [Fact]
    public void FromBrookKeyCreatesValidKey()
    {
        BrookKey brookKey = new("TestBrook", "entity-1");
        BrookPosition version = new(42);
        UxProjectionVersionedCacheKey key = UxProjectionVersionedCacheKey.FromBrookKey(brookKey, version);
        Assert.Equal("TestBrook", key.BrookName);
        Assert.Equal("entity-1", key.EntityId);
        Assert.Equal(42, key.Version.Value);
    }

    /// <summary>
    ///     FromCursorKey should create key from cursor key and version.
    /// </summary>
    [Fact]
    public void FromCursorKeyCreatesValidKey()
    {
        UxProjectionCursorKey cursorKey = new("TestBrook", "entity-1");
        BrookPosition version = new(42);
        UxProjectionVersionedCacheKey key = UxProjectionVersionedCacheKey.FromCursorKey(cursorKey, version);
        Assert.Equal("TestBrook", key.BrookName);
        Assert.Equal("entity-1", key.EntityId);
        Assert.Equal(42, key.Version.Value);
    }

    /// <summary>
    ///     GetHashCode should be consistent for equal keys.
    /// </summary>
    [Fact]
    public void GetHashCodeIsConsistentForEqualKeys()
    {
        BrookPosition version = new(42);
        UxProjectionVersionedCacheKey key1 = new("TestBrook", "entity-1", version);
        UxProjectionVersionedCacheKey key2 = new("TestBrook", "entity-1", version);
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    ///     Implicit conversion to string should work.
    /// </summary>
    [Fact]
    public void ImplicitConversionToStringWorks()
    {
        BrookPosition version = new(42);
        UxProjectionVersionedCacheKey key = new("TestBrook", "entity-1", version);
        string result = key;
        Assert.Equal("TestBrook|entity-1|42", result);
    }

    /// <summary>
    ///     Parse should create key from valid string.
    /// </summary>
    [Fact]
    public void ParseCreatesKeyFromValidString()
    {
        UxProjectionVersionedCacheKey key = UxProjectionVersionedCacheKey.Parse("TestBrook|entity-1|42");
        Assert.Equal("TestBrook", key.BrookName);
        Assert.Equal("entity-1", key.EntityId);
        Assert.Equal(42, key.Version.Value);
    }

    /// <summary>
    ///     Parse should throw when string format is invalid.
    /// </summary>
    [Fact]
    public void ParseThrowsWhenFormatIsInvalid()
    {
        Assert.Throws<FormatException>(() => UxProjectionVersionedCacheKey.Parse("invalid-format"));
    }

    /// <summary>
    ///     Parse should throw when string is null.
    /// </summary>
    [Fact]
    public void ParseThrowsWhenStringIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => UxProjectionVersionedCacheKey.Parse(null!));
    }

    /// <summary>
    ///     Parse should throw when version is not a number.
    /// </summary>
    [Fact]
    public void ParseThrowsWhenVersionIsNotNumber()
    {
        Assert.Throws<FormatException>(() => UxProjectionVersionedCacheKey.Parse("TestBrook|entity-1|abc"));
    }

    /// <summary>
    ///     ToBrookKey should return correct BrookKey.
    /// </summary>
    [Fact]
    public void ToBrookKeyReturnsCorrectBrookKey()
    {
        BrookPosition version = new(42);
        UxProjectionVersionedCacheKey key = new("TestBrook", "entity-1", version);
        BrookKey brookKey = key.ToBrookKey();
        Assert.Equal("TestBrook", brookKey.BrookName);
        Assert.Equal("entity-1", brookKey.EntityId);
    }

    /// <summary>
    ///     ToCursorKey should return correct cursor key.
    /// </summary>
    [Fact]
    public void ToCursorKeyReturnsCorrectCursorKey()
    {
        BrookPosition version = new(42);
        UxProjectionVersionedCacheKey key = new("TestBrook", "entity-1", version);
        UxProjectionCursorKey cursorKey = key.ToCursorKey();
        Assert.Equal("TestBrook", cursorKey.BrookName);
        Assert.Equal("entity-1", cursorKey.EntityId);
    }

    /// <summary>
    ///     ToString should return string representation.
    /// </summary>
    [Fact]
    public void ToStringReturnsStringRepresentation()
    {
        BrookPosition version = new(42);
        UxProjectionVersionedCacheKey key = new("TestBrook", "entity-1", version);
        Assert.Equal("TestBrook|entity-1|42", key.ToString());
    }

    /// <summary>
    ///     TryParse should fail for empty string.
    /// </summary>
    [Fact]
    public void TryParseFailsForEmptyString()
    {
        bool result = UxProjectionVersionedCacheKey.TryParse(string.Empty, out UxProjectionVersionedCacheKey key);
        Assert.False(result);
        Assert.Equal(default, key);
    }

    /// <summary>
    ///     TryParse should fail for invalid version.
    /// </summary>
    [Fact]
    public void TryParseFailsForInvalidVersion()
    {
        bool result = UxProjectionVersionedCacheKey.TryParse(
            "TestBrook|entity-1|abc",
            out UxProjectionVersionedCacheKey key);
        Assert.False(result);
        Assert.Equal(default, key);
    }

    /// <summary>
    ///     TryParse should fail for null string.
    /// </summary>
    [Fact]
    public void TryParseFailsForNullString()
    {
        bool result = UxProjectionVersionedCacheKey.TryParse(null, out UxProjectionVersionedCacheKey key);
        Assert.False(result);
        Assert.Equal(default, key);
    }

    /// <summary>
    ///     TryParse should fail for wrong number of parts.
    /// </summary>
    [Fact]
    public void TryParseFailsForWrongNumberOfParts()
    {
        bool result = UxProjectionVersionedCacheKey.TryParse(
            "TestBrook|entity-1",
            out UxProjectionVersionedCacheKey key);
        Assert.False(result);
        Assert.Equal(default, key);
    }

    /// <summary>
    ///     TryParse should succeed for valid string.
    /// </summary>
    [Fact]
    public void TryParseSucceedsForValidString()
    {
        bool result = UxProjectionVersionedCacheKey.TryParse(
            "TestBrook|entity-1|42",
            out UxProjectionVersionedCacheKey key);
        Assert.True(result);
        Assert.Equal("TestBrook", key.BrookName);
        Assert.Equal("entity-1", key.EntityId);
        Assert.Equal(42, key.Version.Value);
    }

    /// <summary>
    ///     Version 0 should be valid.
    /// </summary>
    [Fact]
    public void VersionZeroIsValid()
    {
        BrookPosition version = new(0);
        UxProjectionVersionedCacheKey key = new("TestBrook", "entity-1", version);
        Assert.Equal(0, key.Version.Value);
    }
}