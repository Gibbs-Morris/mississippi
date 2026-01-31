using System;


namespace Mississippi.EventSourcing.Brooks.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="BrookRangeKey" /> parsing, composite helpers and range calculations.
/// </summary>
public sealed class BrookRangeKeyTests
{
    /// <summary>
    ///     Composite key helpers should round trip between BrookKey and BrookRangeKey.
    /// </summary>
    [Fact]
    public void CompositeKeyHelpersRoundTrip()
    {
        BrookKey bk = new("tp", "id");
        BrookRangeKey rk = BrookRangeKey.FromBrookCompositeKey(bk, 2, 5);
        Assert.Equal("tp", rk.BrookName);
        Assert.Equal("id", rk.EntityId);
        Assert.Equal(2, rk.Start.Value);
        Assert.Equal(5, rk.Count);
        BrookKey asKey = rk.ToBrookCompositeKey();
        Assert.Equal(bk.BrookName, asKey.BrookName);
        Assert.Equal(bk.EntityId, asKey.EntityId);
        string s = rk;
        Assert.Equal(BrookRangeKey.FromBrookRangeKey(rk), s);
    }

    /// <summary>
    ///     Constructor should accept components that exactly reach the maximum combined length threshold.
    /// </summary>
    [Fact]
    public void ConstructorAllowsExactMaxLength()
    {
        // Create components that sum to exactly 4192 characters
        // type.Length + id.Length + start.ToString().Length + count.ToString().Length + 3 = 4192
        // Using start=0 (1 char), count=0 (1 char), we need type + id = 4187
        string type = new('x', 2093);
        string id = new('y', 2094);
        BrookRangeKey key = new(type, id, 0, 0);
        Assert.Equal(type, key.BrookName);
        Assert.Equal(id, key.EntityId);
    }

    /// <summary>
    ///     Constructor should allow composite keys that sit just below the maximum length threshold.
    /// </summary>
    [Fact]
    public void ConstructorAllowsValuesBelowLengthLimit()
    {
        string type = new('t', 1000);
        BrookRangeKey key = new(type, "i", 12_345, 67_890);
        Assert.Equal(type, key.BrookName);
    }

    /// <summary>
    ///     Constructor should reject id containing separator character.
    /// </summary>
    [Fact]
    public void ConstructorRejectsIdWithSeparator()
    {
        Assert.Throws<ArgumentException>(() => new BrookRangeKey("type", "i|d", 0, 1));
    }

    /// <summary>
    ///     Constructor should reject components that exceed the maximum combined length by exactly one character.
    /// </summary>
    [Fact]
    public void ConstructorRejectsMaxLengthPlusOne()
    {
        // Create components that sum to 4193 characters
        // type.Length + id.Length + start.ToString().Length + count.ToString().Length + 3 = 4193
        // Using start=0 (1 char), count=0 (1 char), we need type + id = 4188
        string type = new('x', 2094);
        string id = new('y', 2094);
        Assert.Throws<ArgumentException>(() => new BrookRangeKey(type, id, 0, 0));
    }

    /// <summary>
    ///     Constructor should reject negative count parameter.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNegativeCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrookRangeKey("type", "id", 0, -1));
    }

    /// <summary>
    ///     Constructor should reject negative start parameter.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNegativeStart()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrookRangeKey("type", "id", -1, 1));
    }

    /// <summary>
    ///     Constructor should reject null id parameter.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNullId()
    {
        Assert.Throws<ArgumentNullException>(() => new BrookRangeKey("type", null!, 0, 1));
    }

    /// <summary>
    ///     Constructor should reject null type parameter.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNullType()
    {
        Assert.Throws<ArgumentNullException>(() => new BrookRangeKey(null!, "id", 0, 1));
    }

    /// <summary>
    ///     Constructor should reject type containing separator character.
    /// </summary>
    [Fact]
    public void ConstructorRejectsTypeWithSeparator()
    {
        Assert.Throws<ArgumentException>(() => new BrookRangeKey("ty|pe", "id", 0, 1));
    }

    /// <summary>
    ///     End should be Start - 1 when the range count is zero.
    /// </summary>
    [Fact]
    public void EndIsStartMinusOneWhenCountIsZero()
    {
        BrookRangeKey rk = new("a", "b", 10, 0);
        Assert.Equal(9, rk.End.Value);
    }

    /// <summary>
    ///     End should be Start + Count - 1.
    /// </summary>
    [Fact]
    public void EndIsStartPlusCountMinusOne()
    {
        BrookRangeKey rk = new("a", "b", 10, 7);
        Assert.Equal(16, rk.End.Value);
    }

    /// <summary>
    ///     FromString should throw for malformed inputs that do not contain four parts or contain non-numeric ranges.
    /// </summary>
    [Fact]
    public void FromStringBadFormatThrows()
    {
        Assert.Throws<FormatException>(() => BrookRangeKey.FromString("too|few|parts"));
        Assert.Throws<FormatException>(() => BrookRangeKey.FromString("a|b|notanumber|1"));
        Assert.Throws<FormatException>(() => BrookRangeKey.FromString("a|b|1|notanumber"));
    }

    /// <summary>
    ///     FromString should parse valid composite keys and preserve component values.
    /// </summary>
    [Fact]
    public void FromStringParsesValidCompositeKey()
    {
        BrookRangeKey parsed = BrookRangeKey.FromString("alpha|beta|7|13");
        Assert.Equal("alpha", parsed.BrookName);
        Assert.Equal("beta", parsed.EntityId);
        Assert.Equal(7, parsed.Start.Value);
        Assert.Equal(13, parsed.Count);
    }

    /// <summary>
    ///     FromString should throw when passed null and constructor enforces component lengths.
    /// </summary>
    [Fact]
    public void FromStringWhenNullAndConstructorEnforceLength()
    {
        Assert.Throws<ArgumentNullException>(() => BrookRangeKey.FromString(null!));

        // Combined key exceeds 4192: 4192 (type) + 1 (separator) + ... > 4192
        string longType = new('x', 4192);
        Assert.Throws<ArgumentException>(() => new BrookRangeKey(longType, string.Empty, 0, 0));
    }

    /// <summary>
    ///     FromString should throw when more than three separators are present.
    /// </summary>
    [Fact]
    public void FromStringWithTooManySeparatorsThrows()
    {
        Assert.Throws<FormatException>(() => BrookRangeKey.FromString("a|b|1|2|extra"));
    }

    /// <summary>
    ///     ToString should return "type|id|start|count".
    /// </summary>
    [Fact]
    public void ToStringUsesSeparatorAndFields()
    {
        BrookRangeKey rk = new("t", "i", 5, 10);
        Assert.Equal("t|i|5|10", rk.ToString());
    }
}