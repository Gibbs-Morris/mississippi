using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Abstractions;


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
    ///     Constructor should create key with valid components.
    /// </summary>
    [Fact]
    public void ConstructorCreatesValidKey()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey key = new("ProjectionName", brookKey);
        Assert.Equal("ProjectionName", key.ProjectionTypeName);
        Assert.Equal(brookKey, key.BrookKey);
    }

    /// <summary>
    ///     Constructor should throw when composite key exceeds maximum length.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenKeyExceedsMaxLength()
    {
        // UxProjectionKey limit is 2048, BrookKey limit is 1024
        // Create a valid BrookKey that's under 1024 chars, but projection type name + brook key exceeds 2048
        string longTypeName = new('x', 1500);
        string brookType = new('y', 300);
        string entityId = new('z', 300);
        BrookKey brookKey = new(brookType, entityId);
        Assert.Throws<ArgumentException>(() => new UxProjectionKey(longTypeName, brookKey));
    }

    /// <summary>
    ///     Constructor should throw when projection type name contains separator.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenProjectionTypeNameContainsSeparator()
    {
        BrookKey brookKey = new("brookType", "entityId");
        Assert.Throws<ArgumentException>(() => new UxProjectionKey("Projection|Name", brookKey));
    }

    /// <summary>
    ///     Constructor should throw when projection type name is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenProjectionTypeNameIsNull()
    {
        BrookKey brookKey = new("brookType", "entityId");
        Assert.Throws<ArgumentNullException>(() => new UxProjectionKey(null!, brookKey));
    }

    /// <summary>
    ///     Equality comparison should work for identical keys.
    /// </summary>
    [Fact]
    public void EqualityWorksForIdenticalKeys()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey key1 = new("ProjectionName", brookKey);
        UxProjectionKey key2 = new("ProjectionName", brookKey);
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    /// <summary>
    ///     FromString should parse valid key string.
    /// </summary>
    [Fact]
    public void FromStringParsesValidKey()
    {
        UxProjectionKey key = UxProjectionKey.FromString("ProjectionName|brookType|entityId");
        Assert.Equal("ProjectionName", key.ProjectionTypeName);
        Assert.Equal("brookType", key.BrookKey.Type);
        Assert.Equal("entityId", key.BrookKey.Id);
    }

    /// <summary>
    ///     FromString should throw when brook key part is invalid.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenBrookKeyPartInvalid()
    {
        Assert.Throws<FormatException>(() => UxProjectionKey.FromString("ProjectionName|invalidBrookKey"));
    }

    /// <summary>
    ///     FromString should throw when format is invalid.
    /// </summary>
    [Fact]
    public void FromStringThrowsWhenFormatInvalid()
    {
        Assert.Throws<FormatException>(() => UxProjectionKey.FromString("invalid-no-separator"));
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
    ///     FromUxProjectionKey returns string representation.
    /// </summary>
    [Fact]
    public void FromUxProjectionKeyReturnsString()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey key = new("ProjectionName", brookKey);
        string result = UxProjectionKey.FromUxProjectionKey(key);
        Assert.Equal("ProjectionName|brookType|entityId", result);
    }

    /// <summary>
    ///     GetHashCode should be consistent for equal keys.
    /// </summary>
    [Fact]
    public void GetHashCodeIsConsistentForEqualKeys()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey key1 = new("ProjectionName", brookKey);
        UxProjectionKey key2 = new("ProjectionName", brookKey);
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    ///     Implicit string conversion should work correctly.
    /// </summary>
    [Fact]
    public void ImplicitStringConversionWorks()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey key = new("ProjectionName", brookKey);
        string result = key;
        Assert.Equal("ProjectionName|brookType|entityId", result);
    }

    /// <summary>
    ///     Implicit string to UxProjectionKey conversion should work.
    /// </summary>
    [Fact]
    public void ImplicitStringToKeyConversionWorks()
    {
        UxProjectionKey key = "ProjectionName|brookType|entityId";
        Assert.Equal("ProjectionName", key.ProjectionTypeName);
        Assert.Equal("brookType", key.BrookKey.Type);
        Assert.Equal("entityId", key.BrookKey.Id);
    }

    /// <summary>
    ///     Inequality comparison should work for different brook keys.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentBrookKeys()
    {
        UxProjectionKey key1 = new("ProjectionName", new("brookType", "entity1"));
        UxProjectionKey key2 = new("ProjectionName", new("brookType", "entity2"));
        Assert.NotEqual(key1, key2);
    }

    /// <summary>
    ///     Inequality comparison should work for different projection type names.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentProjectionTypes()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey key1 = new("ProjectionA", brookKey);
        UxProjectionKey key2 = new("ProjectionB", brookKey);
        Assert.NotEqual(key1, key2);
        Assert.False(key1 == key2);
        Assert.True(key1 != key2);
    }

    /// <summary>
    ///     Roundtrip through string and back preserves key.
    /// </summary>
    [Fact]
    public void RoundtripThroughStringPreservesKey()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey original = new("ProjectionName", brookKey);
        string stringForm = original;
        UxProjectionKey parsed = stringForm;
        Assert.Equal(original, parsed);
    }

    /// <summary>
    ///     ToString should return correct format.
    /// </summary>
    [Fact]
    public void ToStringReturnsCorrectFormat()
    {
        BrookKey brookKey = new("brookType", "entityId");
        UxProjectionKey key = new("ProjectionName", brookKey);
        Assert.Equal("ProjectionName|brookType|entityId", key.ToString());
    }
}