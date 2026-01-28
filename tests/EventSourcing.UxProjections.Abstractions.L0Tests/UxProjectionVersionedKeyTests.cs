using System;


using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionVersionedKey" /> behavior.
/// </summary>
public sealed class UxProjectionVersionedKeyTests
{
    /// <summary>
    ///     Constructor should create key with valid projection key and version.
    /// </summary>
    [Fact]
    public void ConstructorCreatesValidKey()
    {
        UxProjectionKey projectionKey = new("entity-1");
        BrookPosition version = new(42);
        UxProjectionVersionedKey key = new(projectionKey, version);
        Assert.Equal(projectionKey, key.ProjectionKey);
        Assert.Equal(version, key.Version);
    }

    /// <summary>
    ///     Constructor should throw when composite key exceeds maximum length.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenKeyExceedsMaxLength()
    {
        // UxProjectionVersionedKey max = 4192 chars: entityId|version
        // Create an entity ID that when combined with version exceeds limit
        string longEntityId = new('e', 4180);
        UxProjectionKey projectionKey = new(longEntityId);
        BrookPosition version = new(999999999999); // 12 digits + separator = 13 chars, total > 4192
        Assert.Throws<ArgumentException>(() => new UxProjectionVersionedKey(projectionKey, version));
    }

    /// <summary>
    ///     Constructor should throw when version is NotSet (-1).
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenVersionIsNotSet()
    {
        UxProjectionKey projectionKey = new("entity-1");
        BrookPosition notSetVersion = new(-1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new UxProjectionVersionedKey(projectionKey, notSetVersion));
    }

    /// <summary>
    ///     Equality comparison should work for identical keys.
    /// </summary>
    [Fact]
    public void EqualityWorksForIdenticalKeys()
    {
        UxProjectionKey projectionKey = new("entity-1");
        UxProjectionVersionedKey key1 = new(projectionKey, new(42));
        UxProjectionVersionedKey key2 = new(projectionKey, new(42));
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    /// <summary>
    ///     FromString should parse valid versioned key string.
    /// </summary>
    [Fact]
    public void FromStringParsesValidKey()
    {
        UxProjectionVersionedKey key = UxProjectionVersionedKey.FromString("entity-1|42");
        Assert.Equal("entity-1", key.ProjectionKey.EntityId);
        Assert.Equal(42, key.Version.Value);
    }

    /// <summary>
    ///     FromString should throw when format is missing version separator.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenMissingVersionSeparator()
    {
        Assert.Throws<FormatException>(() => UxProjectionVersionedKey.FromString("NoSeparators"));
    }

    /// <summary>
    ///     FromString should throw when value is null.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => UxProjectionVersionedKey.FromString(null!));
    }

    /// <summary>
    ///     FromString should throw when version is negative.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenVersionIsNegative()
    {
        Assert.Throws<FormatException>(() => UxProjectionVersionedKey.FromString("entity-1|-1"));
    }

    /// <summary>
    ///     FromString should throw when version is not a valid integer.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenVersionIsNotInteger()
    {
        Assert.Throws<FormatException>(() => UxProjectionVersionedKey.FromString("entity-1|notANumber"));
    }

    /// <summary>
    ///     FromUxProjectionVersionedKey should return string representation.
    /// </summary>
    [Fact]
    public void FromUxProjectionVersionedKeyReturnsString()
    {
        UxProjectionKey projectionKey = new("entity-1");
        UxProjectionVersionedKey versionedKey = new(projectionKey, new(42));
        string result = UxProjectionVersionedKey.FromUxProjectionVersionedKey(versionedKey);
        Assert.Equal("entity-1|42", result);
    }

    /// <summary>
    ///     GetHashCode should be consistent for equal keys.
    /// </summary>
    [Fact]
    public void GetHashCodeIsConsistentForEqualKeys()
    {
        UxProjectionKey projectionKey = new("entity-1");
        UxProjectionVersionedKey key1 = new(projectionKey, new(42));
        UxProjectionVersionedKey key2 = new(projectionKey, new(42));
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    ///     Implicit string conversion should work correctly.
    /// </summary>
    [Fact]
    public void ImplicitStringConversionWorks()
    {
        UxProjectionKey projectionKey = new("entity-1");
        UxProjectionVersionedKey versionedKey = new(projectionKey, new(42));
        string result = versionedKey;
        Assert.Equal("entity-1|42", result);
    }

    /// <summary>
    ///     Implicit string to UxProjectionVersionedKey conversion should work.
    /// </summary>
    [Fact]
    public void ImplicitStringToKeyConversionWorks()
    {
        UxProjectionVersionedKey key = "entity-1|42";
        Assert.Equal("entity-1", key.ProjectionKey.EntityId);
        Assert.Equal(42, key.Version.Value);
    }

    /// <summary>
    ///     Inequality comparison should work for different projection keys.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentProjectionKeys()
    {
        UxProjectionKey projectionKey1 = new("entity-1");
        UxProjectionKey projectionKey2 = new("entity-2");
        UxProjectionVersionedKey key1 = new(projectionKey1, new(42));
        UxProjectionVersionedKey key2 = new(projectionKey2, new(42));
        Assert.NotEqual(key1, key2);
    }

    /// <summary>
    ///     Inequality comparison should work for different versions.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentVersions()
    {
        UxProjectionKey projectionKey = new("entity-1");
        UxProjectionVersionedKey key1 = new(projectionKey, new(42));
        UxProjectionVersionedKey key2 = new(projectionKey, new(100));
        Assert.NotEqual(key1, key2);
        Assert.False(key1 == key2);
        Assert.True(key1 != key2);
    }

    /// <summary>
    ///     Large version values should be handled correctly.
    /// </summary>
    [Fact]
    public void LargeVersionValuesHandledCorrectly()
    {
        UxProjectionKey projectionKey = new("e");
        BrookPosition largeVersion = new(long.MaxValue);
        UxProjectionVersionedKey key = new(projectionKey, largeVersion);
        Assert.Equal(long.MaxValue, key.Version.Value);
        string stringForm = key;
        UxProjectionVersionedKey parsed = UxProjectionVersionedKey.FromString(stringForm);
        Assert.Equal(long.MaxValue, parsed.Version.Value);
    }

    /// <summary>
    ///     Roundtrip through string and back preserves key.
    /// </summary>
    [Fact]
    public void RoundtripThroughStringPreservesKey()
    {
        UxProjectionKey projectionKey = new("entity-1");
        UxProjectionVersionedKey original = new(projectionKey, new(42));
        string stringForm = original;
        UxProjectionVersionedKey parsed = stringForm;
        Assert.Equal(original, parsed);
    }

    /// <summary>
    ///     Version with zero value should be valid.
    /// </summary>
    [Fact]
    public void VersionZeroIsValid()
    {
        UxProjectionKey projectionKey = new("entity-1");
        BrookPosition zeroVersion = new(0);
        UxProjectionVersionedKey key = new(projectionKey, zeroVersion);
        Assert.Equal(0, key.Version.Value);
    }
}