using System;



namespace Mississippi.EventSourcing.Brooks.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="BrookKey" /> behavior.
/// </summary>
public sealed class BrookKeyTests
{
    /// <summary>
    ///     Component values containing the separator are rejected.
    /// </summary>
    [Fact]
    public void ComponentWithSeparatorThrows()
    {
        Assert.Throws<ArgumentException>(() => new BrookKey("a|b", "id"));
        Assert.Throws<ArgumentException>(() => new BrookKey("type", "i|d"));
    }

    /// <summary>
    ///     Constructor should accept components that exactly reach the maximum combined length threshold.
    /// </summary>
    [Fact]
    public void ConstructorAllowsExactMaxLength()
    {
        // Create components that sum to exactly 4192 characters (4191 + 1 separator)
        // type.Length + id.Length + 1 = 4192
        string type = new('x', 2096);
        string id = new('y', 2095);
        BrookKey key = new(type, id);
        Assert.Equal(type, key.BrookName);
        Assert.Equal(id, key.EntityId);
    }

    /// <summary>
    ///     Verifies that valid components create a key and string form.
    /// </summary>
    [Fact]
    public void ConstructorCreatesKey()
    {
        BrookKey key = new("type", "id");
        Assert.Equal("type", key.BrookName);
        Assert.Equal("id", key.EntityId);
        Assert.Equal("type|id", key.ToString());
    }

    /// <summary>
    ///     Passing null components should throw ArgumentNullException.
    /// </summary>
    /// <param name="type">The type component to pass to the constructor (may be null for the test).</param>
    /// <param name="id">The id component to pass to the constructor (may be null for the test).</param>
    [Theory]
    [InlineData(null, "a")]
    [InlineData("a", null)]
    public void ConstructorNullThrows(
        string? type,
        string? id
    )
    {
        Assert.Throws<ArgumentNullException>(() => new BrookKey(type!, id!));
    }

    /// <summary>
    ///     Constructor should reject components that exceed the maximum combined length by exactly one character.
    /// </summary>
    [Fact]
    public void ConstructorRejectsMaxLengthPlusOne()
    {
        // Create components that sum to 4193 characters (4192 + 1 separator)
        // type.Length + id.Length + 1 = 4193
        string type = new('x', 2096);
        string id = new('y', 2096);
        Assert.Throws<ArgumentException>(() => new BrookKey(type, id));
    }

    /// <summary>
    ///     Constructor should enforce the maximum combined component length.
    /// </summary>
    [Fact]
    public void ConstructorWhenCombinedLengthTooLongThrows()
    {
        // Combined key exceeds 4192: 4192 (type) + 1 (separator) + 0 (id) = 4193
        string longType = new('x', 4192);
        Assert.Throws<ArgumentException>(() => new BrookKey(longType, string.Empty));
    }

    /// <summary>
    ///     FromBrookKey should return the same string representation as ToString.
    /// </summary>
    [Fact]
    public void FromBrookKeyReturnsStringRepresentation()
    {
        BrookKey k = new("A", "B");
        string s = BrookKey.FromBrookKey(k);
        Assert.Equal("A|B", s);
    }

    /// <summary>
    ///     FromString should allow empty type components when the separator is the leading character.
    /// </summary>
    [Fact]
    public void FromStringAllowsEmptyType()
    {
        BrookKey key = BrookKey.FromString("|identifier");
        Assert.Equal(string.Empty, key.BrookName);
        Assert.Equal("identifier", key.EntityId);
    }

    /// <summary>
    ///     Malformed string input should throw a FormatException.
    /// </summary>
    [Fact]
    public void FromStringBadFormatThrows()
    {
        Assert.Throws<FormatException>(() => BrookKey.FromString("no-separator"));
    }

    /// <summary>
    ///     FromString should throw when passed null.
    /// </summary>
    [Fact]
    public void FromStringWhenNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => BrookKey.FromString(null!));
    }

    /// <summary>
    ///     Verifies implicit conversion to string and FromString parsing.
    /// </summary>
    [Fact]
    public void ImplicitToStringAndFromStringWork()
    {
        BrookKey k = new("t", "i");
        string s = k; // implicit to string
        Assert.Equal("t|i", s);
        BrookKey parsed = BrookKey.FromString("t|i");
        Assert.Equal("t", parsed.BrookName);
        Assert.Equal("i", parsed.EntityId);
    }

    /// <summary>
    ///     ToString returns the expected representation.
    /// </summary>
    [Fact]
    public void ToStringUsesSeparator()
    {
        BrookKey k = new("X", "Y");
        Assert.Equal("X|Y", k.ToString());
    }
}