using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionVersionedKey" /> behavior.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections Abstractions")]
[AllureSubSuite("UxProjectionVersionedKey")]
public sealed class UxProjectionVersionedKeyTests
{
    /// <summary>
    ///     A test grain type for testing purposes.
    /// </summary>
    [BrookName("TEST", "VERSIONED", "BROOK")]
    private sealed class TestGrain
    {
    }

    /// <summary>
    ///     A test projection type for testing purposes.
    /// </summary>
    /// <param name="Value">The sample value.</param>
    private sealed record TestProjectionType(int Value);

    /// <summary>
    ///     Constructor should create key with valid projection key and version.
    /// </summary>
    [Fact]
    public void ConstructorCreatesValidKey()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey projectionKey = new("ProjectionName", brookKey);
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
        // UxProjectionKey max = 2048 chars: typeName|brookType|entityId
        // UxProjectionVersionedKey max = 2048 chars: projectionKey|version
        // To trigger the exception in UxProjectionVersionedKey (not UxProjectionKey):
        // - Create a UxProjectionKey at max length (2048)
        // - Add a version which pushes it over
        // brookKey "b|i" = 3 chars, so typeName can be 2044 chars
        // UxProjectionKey string = 2044 + 1 + 3 = 2048 (at limit, valid)
        // Adding version "999999999999" (12 chars) + separator = 13 chars
        // Total = 2048 + 13 = 2061 > 2048 (exceeds limit)
        string maxTypeName = new('t', 2044);
        BrookKey tinyBrook = new("b", "i");
        UxProjectionKey maxProjectionKey = new(maxTypeName, tinyBrook);
        BrookPosition version = new(999999999999);
        Assert.Throws<ArgumentException>(() => new UxProjectionVersionedKey(maxProjectionKey, version));
    }

    /// <summary>
    ///     Constructor should throw when version is NotSet (-1).
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenVersionIsNotSet()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey projectionKey = new("ProjectionName", brookKey);
        BrookPosition notSetVersion = new(-1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new UxProjectionVersionedKey(projectionKey, notSetVersion));
    }

    /// <summary>
    ///     Equality comparison should work for identical keys.
    /// </summary>
    [Fact]
    public void EqualityWorksForIdenticalKeys()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey projectionKey = new("ProjectionName", brookKey);
        UxProjectionVersionedKey key1 = new(projectionKey, new(42));
        UxProjectionVersionedKey key2 = new(projectionKey, new(42));
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    /// <summary>
    ///     ForGrain method should create key for specific projection type, grain, and version.
    /// </summary>
    [Fact]
    public void ForGrainCreatesKeyWithCorrectComponents()
    {
        BrookPosition version = new(100);
        UxProjectionVersionedKey key =
            UxProjectionVersionedKey.ForGrain<TestProjectionType, TestGrain>("entity-1", version);
        Assert.Equal("TestProjectionType", key.ProjectionKey.ProjectionTypeName);
        Assert.Equal("TEST.VERSIONED.BROOK", key.ProjectionKey.BrookKey.Type);
        Assert.Equal("entity-1", key.ProjectionKey.BrookKey.Id);
        Assert.Equal(100, key.Version.Value);
    }

    /// <summary>
    ///     FromString should parse valid versioned key string.
    /// </summary>
    [Fact]
    public void FromStringParsesValidKey()
    {
        UxProjectionVersionedKey key = UxProjectionVersionedKey.FromString("ProjectionName|brookType|entityId|42");
        Assert.Equal("ProjectionName", key.ProjectionKey.ProjectionTypeName);
        Assert.Equal("brookType", key.ProjectionKey.BrookKey.Type);
        Assert.Equal("entityId", key.ProjectionKey.BrookKey.Id);
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
    ///     FromString should throw when projection key part is invalid.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenProjectionKeyPartInvalid()
    {
        Assert.Throws<FormatException>(() => UxProjectionVersionedKey.FromString("InvalidProjectionKey|42"));
    }

    /// <summary>
    ///     FromString should throw when version is negative.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenVersionIsNegative()
    {
        Assert.Throws<FormatException>(() =>
            UxProjectionVersionedKey.FromString("ProjectionName|brookType|entityId|-1"));
    }

    /// <summary>
    ///     FromString should throw when version is not a valid integer.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenVersionIsNotInteger()
    {
        Assert.Throws<FormatException>(() =>
            UxProjectionVersionedKey.FromString("ProjectionName|brookType|entityId|notANumber"));
    }

    /// <summary>
    ///     FromUxProjectionVersionedKey should return string representation.
    /// </summary>
    [Fact]
    public void FromUxProjectionVersionedKeyReturnsString()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey projectionKey = new("ProjectionName", brookKey);
        UxProjectionVersionedKey versionedKey = new(projectionKey, new(42));
        string result = UxProjectionVersionedKey.FromUxProjectionVersionedKey(versionedKey);
        Assert.Equal("ProjectionName|brookType|entityId|42", result);
    }

    /// <summary>
    ///     GetHashCode should be consistent for equal keys.
    /// </summary>
    [Fact]
    public void GetHashCodeIsConsistentForEqualKeys()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey projectionKey = new("ProjectionName", brookKey);
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
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey projectionKey = new("ProjectionName", brookKey);
        UxProjectionVersionedKey versionedKey = new(projectionKey, new(42));
        string result = versionedKey;
        Assert.Equal("ProjectionName|brookType|entityId|42", result);
    }

    /// <summary>
    ///     Implicit string to UxProjectionVersionedKey conversion should work.
    /// </summary>
    [Fact]
    public void ImplicitStringToKeyConversionWorks()
    {
        UxProjectionVersionedKey key = "ProjectionName|brookType|entityId|42";
        Assert.Equal("ProjectionName", key.ProjectionKey.ProjectionTypeName);
        Assert.Equal("brookType", key.ProjectionKey.BrookKey.Type);
        Assert.Equal("entityId", key.ProjectionKey.BrookKey.Id);
        Assert.Equal(42, key.Version.Value);
    }

    /// <summary>
    ///     Inequality comparison should work for different projection keys.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentProjectionKeys()
    {
        BrookKey brookKey1 = new("brookType", "entity1");
        BrookKey brookKey2 = new("brookType", "entity2");
        UxProjectionKey projectionKey1 = new("ProjectionName", brookKey1);
        UxProjectionKey projectionKey2 = new("ProjectionName", brookKey2);
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
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey projectionKey = new("ProjectionName", brookKey);
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
        BrookKey brookKey = new("t", "e");
        UxProjectionKey projectionKey = new("P", brookKey);
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
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey projectionKey = new("ProjectionName", brookKey);
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
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey projectionKey = new("ProjectionName", brookKey);
        BrookPosition zeroVersion = new(0);
        UxProjectionVersionedKey key = new(projectionKey, zeroVersion);
        Assert.Equal(0, key.Version.Value);
    }
}