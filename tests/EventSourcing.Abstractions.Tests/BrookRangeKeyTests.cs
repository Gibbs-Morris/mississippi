namespace Mississippi.EventSourcing.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="BrookRangeKey" /> parsing, composite helpers and range calculations.
/// </summary>
public sealed class BrookRangeKeyTests
{
    /// <summary>
    ///     FromString should throw when passed null and constructor enforces component lengths.
    /// </summary>
    [Fact]
    public void FromStringWhenNullAndConstructorEnforceLength()
    {
        Assert.Throws<ArgumentNullException>(() => BrookRangeKey.FromString(null!));
        string longType = new('x', 1024);
        Assert.Throws<ArgumentException>(() => new BrookRangeKey(longType, string.Empty, 0, 0));
    }

    /// <summary>
    ///     Composite key helpers should round trip between BrookKey and BrookRangeKey.
    /// </summary>
    [Fact]
    public void CompositeKeyHelpersRoundTrip()
    {
        BrookKey bk = new("tp", "id");
        BrookRangeKey rk = BrookRangeKey.FromBrookCompositeKey(bk, 2, 5);
        Assert.Equal("tp", rk.Type);
        Assert.Equal("id", rk.Id);
        Assert.Equal(2, rk.Start.Value);
        Assert.Equal(5, rk.Count);
        BrookKey asKey = rk.ToBrookCompositeKey();
        Assert.Equal(bk.Type, asKey.Type);
        Assert.Equal(bk.Id, asKey.Id);
        string s = rk;
        Assert.Equal(BrookRangeKey.FromBrookRangeKey(rk), s);
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
    ///     End should be Start - 1 when the range count is zero.
    /// </summary>
    [Fact]
    public void EndIsStartMinusOneWhenCountIsZero()
    {
        BrookRangeKey rk = new("a", "b", 10, 0);
        Assert.Equal(9, rk.End.Value);
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
    ///     ToString should return "type|id|start|count".
    /// </summary>
    [Fact]
    public void ToStringUsesSeparatorAndFields()
    {
        BrookRangeKey rk = new("t", "i", 5, 10);
        Assert.Equal("t|i|5|10", rk.ToString());
    }

    /// <summary>
    ///     FromString should parse valid composite keys and preserve component values.
    /// </summary>
    [Fact]
    public void FromStringParsesValidCompositeKey()
    {
        BrookRangeKey parsed = BrookRangeKey.FromString("alpha|beta|7|13");
        Assert.Equal("alpha", parsed.Type);
        Assert.Equal("beta", parsed.Id);
        Assert.Equal(7, parsed.Start.Value);
        Assert.Equal(13, parsed.Count);
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
    ///     Constructor should allow composite keys that sit just below the maximum length threshold.
    /// </summary>
    [Fact]
    public void ConstructorAllowsValuesBelowLengthLimit()
    {
        string type = new('t', 1000);
        BrookRangeKey key = new(type, "i", 12_345, 67_890);
        Assert.Equal(type, key.Type);
    }
}