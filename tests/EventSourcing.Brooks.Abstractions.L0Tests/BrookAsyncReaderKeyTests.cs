using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="BrookAsyncReaderKey" /> behavior.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Brook Async Reader Key")]
public sealed class BrookAsyncReaderKeyTests
{
    /// <summary>
    ///     Constructor should create key with valid brook key and instance ID.
    /// </summary>
    [Fact]
    public void ConstructorCreatesValidKey()
    {
        BrookKey brookKey = new("type", "id");
        Guid instanceId = Guid.NewGuid();
        BrookAsyncReaderKey key = new(brookKey, instanceId);
        Assert.Equal(brookKey, key.BrookKey);
        Assert.Equal(instanceId, key.InstanceId);
    }

    /// <summary>
    ///     Create should generate unique instance ID each time.
    /// </summary>
    [Fact]
    public void CreateGeneratesUniqueInstanceId()
    {
        BrookKey brookKey = new("type", "id");
        BrookAsyncReaderKey key1 = BrookAsyncReaderKey.Create(brookKey);
        BrookAsyncReaderKey key2 = BrookAsyncReaderKey.Create(brookKey);
        Assert.Equal(brookKey, key1.BrookKey);
        Assert.Equal(brookKey, key2.BrookKey);
        Assert.NotEqual(key1.InstanceId, key2.InstanceId);
    }

    /// <summary>
    ///     Empty type and id should be allowed in brook key.
    /// </summary>
    [Fact]
    public void EmptyTypeAndIdAllowed()
    {
        BrookKey brookKey = new(string.Empty, string.Empty);
        Guid instanceId = Guid.NewGuid();
        BrookAsyncReaderKey key = new(brookKey, instanceId);
        string stringForm = key;
        BrookAsyncReaderKey parsed = BrookAsyncReaderKey.Parse(stringForm);
        Assert.Equal(string.Empty, parsed.BrookKey.Type);
        Assert.Equal(string.Empty, parsed.BrookKey.Id);
        Assert.Equal(instanceId, parsed.InstanceId);
    }

    /// <summary>
    ///     Equality comparison should work for identical keys.
    /// </summary>
    [Fact]
    public void EqualityWorksForIdenticalKeys()
    {
        BrookKey brookKey = new("type", "id");
        Guid instanceId = Guid.NewGuid();
        BrookAsyncReaderKey key1 = new(brookKey, instanceId);
        BrookAsyncReaderKey key2 = new(brookKey, instanceId);
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    /// <summary>
    ///     FromString should correctly parse valid key string.
    /// </summary>
    [Fact]
    public void FromStringParsesValidKey()
    {
        Guid instanceId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        string keyString = $"type|id|{instanceId:N}";
        BrookAsyncReaderKey key = BrookAsyncReaderKey.FromString(keyString);
        Assert.Equal("type", key.BrookKey.Type);
        Assert.Equal("id", key.BrookKey.Id);
        Assert.Equal(instanceId, key.InstanceId);
    }

    /// <summary>
    ///     GetHashCode should be consistent for equal keys.
    /// </summary>
    [Fact]
    public void GetHashCodeIsConsistentForEqualKeys()
    {
        BrookKey brookKey = new("type", "id");
        Guid instanceId = Guid.NewGuid();
        BrookAsyncReaderKey key1 = new(brookKey, instanceId);
        BrookAsyncReaderKey key2 = new(brookKey, instanceId);
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    /// <summary>
    ///     Implicit string conversion should produce correct format.
    /// </summary>
    [Fact]
    public void ImplicitStringConversionWorks()
    {
        BrookKey brookKey = new("type", "id");
        Guid instanceId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        BrookAsyncReaderKey key = new(brookKey, instanceId);
        string result = key;
        Assert.Equal($"type|id|{instanceId:N}", result);
    }

    /// <summary>
    ///     Implicit string to BrookAsyncReaderKey conversion should work.
    /// </summary>
    [Fact]
    public void ImplicitStringToKeyConversionWorks()
    {
        Guid instanceId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        string keyString = $"type|id|{instanceId:N}";
        BrookAsyncReaderKey key = keyString;
        Assert.Equal("type", key.BrookKey.Type);
        Assert.Equal("id", key.BrookKey.Id);
        Assert.Equal(instanceId, key.InstanceId);
    }

    /// <summary>
    ///     Inequality comparison should work for different brook keys.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentBrookKeys()
    {
        Guid instanceId = Guid.NewGuid();
        BrookAsyncReaderKey key1 = new(new("type1", "id"), instanceId);
        BrookAsyncReaderKey key2 = new(new("type2", "id"), instanceId);
        Assert.NotEqual(key1, key2);
    }

    /// <summary>
    ///     Inequality comparison should work for different instance IDs.
    /// </summary>
    [Fact]
    public void InequalityWorksForDifferentInstanceIds()
    {
        BrookKey brookKey = new("type", "id");
        BrookAsyncReaderKey key1 = new(brookKey, Guid.NewGuid());
        BrookAsyncReaderKey key2 = new(brookKey, Guid.NewGuid());
        Assert.NotEqual(key1, key2);
        Assert.False(key1 == key2);
        Assert.True(key1 != key2);
    }

    /// <summary>
    ///     Parse should correctly parse valid key string.
    /// </summary>
    [Fact]
    public void ParseParsesValidKey()
    {
        Guid instanceId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        string keyString = $"type|id|{instanceId:N}";
        BrookAsyncReaderKey key = BrookAsyncReaderKey.Parse(keyString);
        Assert.Equal("type", key.BrookKey.Type);
        Assert.Equal("id", key.BrookKey.Id);
        Assert.Equal(instanceId, key.InstanceId);
    }

    /// <summary>
    ///     Parse should throw when instance ID is not a valid GUID.
    /// </summary>
    [Fact]
    public void ParseThrowsWhenInstanceIdInvalid()
    {
        Assert.Throws<ArgumentException>(() => BrookAsyncReaderKey.Parse("type|id|notAGuid"));
    }

    /// <summary>
    ///     Parse should throw when missing first separator.
    /// </summary>
    [Fact]
    public void ParseThrowsWhenMissingFirstSeparator()
    {
        Assert.Throws<ArgumentException>(() => BrookAsyncReaderKey.Parse("noSeparatorAtAll"));
    }

    /// <summary>
    ///     Parse should throw when missing second separator.
    /// </summary>
    [Fact]
    public void ParseThrowsWhenMissingSecondSeparator()
    {
        Assert.Throws<ArgumentException>(() => BrookAsyncReaderKey.Parse("type|idWithNoInstanceId"));
    }

    /// <summary>
    ///     Roundtrip through string and back preserves key.
    /// </summary>
    [Fact]
    public void RoundtripThroughStringPreservesKey()
    {
        BrookKey brookKey = new("type", "id");
        Guid instanceId = Guid.NewGuid();
        BrookAsyncReaderKey original = new(brookKey, instanceId);
        string stringForm = original;
        BrookAsyncReaderKey parsed = stringForm;
        Assert.Equal(original, parsed);
    }

    /// <summary>
    ///     ToString should return string representation.
    /// </summary>
    [Fact]
    public void ToStringReturnsStringRepresentation()
    {
        BrookKey brookKey = new("type", "id");
        Guid instanceId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        BrookAsyncReaderKey key = new(brookKey, instanceId);
        string result = key.ToString();
        Assert.Equal($"type|id|{instanceId:N}", result);
    }
}