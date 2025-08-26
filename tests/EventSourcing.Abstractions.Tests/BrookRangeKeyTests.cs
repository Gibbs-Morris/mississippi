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
    ///     End should be Start + Count.
    /// </summary>
    [Fact]
    public void EndIsStartPlusCount()
    {
        BrookRangeKey rk = new("a", "b", 10, 7);
        Assert.Equal(17, rk.End.Value);
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
}