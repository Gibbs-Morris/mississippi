using System;

using Mississippi.Projections.Projections;


namespace Mississippi.Projections.Tests;

/// <summary>
///     Tests for <see cref="VersionedProjectionKey" /> behavior.
/// </summary>
public class VersionedProjectionKeyTests
{
    /// <summary>
    ///     Ensures constructor rejects components containing separators.
    /// </summary>
    [Fact]
    public void ComponentWithSeparatorThrows()
    {
        Assert.Throws<ArgumentException>(() => new VersionedProjectionKey("a|b", "id", 1));
        Assert.Throws<ArgumentException>(() => new VersionedProjectionKey("path", "i|d", 1));
    }

    /// <summary>
    ///     Ensures the constructor accepts components at the limits and preserves the version.
    /// </summary>
    [Fact]
    public void ConstructorAllowsExactMaxLength()
    {
        string path = new('x', 512);
        string id = new('y', 511);
        VersionedProjectionKey key = new(path, id, 123L);
        Assert.Equal(path, key.Path);
        Assert.Equal(id, key.Id);
        Assert.Equal(123L, key.Version);
    }

    /// <summary>
    ///     Ensures null components raise an exception.
    /// </summary>
    /// <param name="path">The path to evaluate.</param>
    /// <param name="id">The id to evaluate.</param>
    [Theory]
    [InlineData(null, "a")]
    [InlineData("a", null)]
    public void ConstructorNullThrows(
        string? path,
        string? id
    )
    {
        Assert.Throws<ArgumentNullException>(() => new VersionedProjectionKey(path!, id!, 1L));
    }

    /// <summary>
    ///     Ensures components exceeding allowed lengths are rejected.
    /// </summary>
    [Fact]
    public void ConstructorRejectsMaxLengthPlusOne()
    {
        string path = new('x', 512);
        string id = new('y', 512);
        Assert.Throws<ArgumentException>(() => new VersionedProjectionKey(path, id, 1L));
    }

    /// <summary>
    ///     Ensures the combined length limit is enforced.
    /// </summary>
    [Fact]
    public void ConstructorWhenCombinedLengthTooLongThrows()
    {
        string longPath = new('x', 1024);
        Assert.Throws<ArgumentException>(() => new VersionedProjectionKey(longPath, string.Empty, 1L));
    }

    /// <summary>
    ///     Ensures invalid text input is rejected.
    /// </summary>
    [Fact]
    public void FromStringBadFormatThrows()
    {
        Assert.Throws<FormatException>(() => VersionedProjectionKey.FromString("no-separator"));
        Assert.Throws<FormatException>(() => VersionedProjectionKey.FromString("p|i"));
        Assert.Throws<FormatException>(() => VersionedProjectionKey.FromString("p|i|not-a-number"));
    }

    /// <summary>
    ///     Ensures parsing extracts all components.
    /// </summary>
    [Fact]
    public void FromStringParsesComponents()
    {
        VersionedProjectionKey key = VersionedProjectionKey.FromString("p|i|5");
        Assert.Equal("p", key.Path);
        Assert.Equal("i", key.Id);
        Assert.Equal(5L, key.Version);
    }

    /// <summary>
    ///     Ensures null text input is not allowed.
    /// </summary>
    [Fact]
    public void FromStringWhenNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => VersionedProjectionKey.FromString(null!));
    }

    /// <summary>
    ///     Ensures formatting helper emits the expected string.
    /// </summary>
    [Fact]
    public void FromVersionedProjectionKeyReturnsStringRepresentation()
    {
        VersionedProjectionKey k = new("A", "B", 42L);
        string s = VersionedProjectionKey.FromVersionedProjectionKey(k);
        Assert.Equal("A|B|42", s);
    }

    /// <summary>
    ///     Ensures implicit conversions round-trip correctly.
    /// </summary>
    [Fact]
    public void ImplicitConversionsWork()
    {
        VersionedProjectionKey k = new("p", "i", 7L);
        string s = k;
        Assert.Equal("p|i|7", s);
        VersionedProjectionKey parsed = "p|i|7";
        Assert.Equal("p", parsed.Path);
        Assert.Equal("i", parsed.Id);
        Assert.Equal(7L, parsed.Version);
    }

    /// <summary>
    ///     Ensures the string conversion uses the separator and includes version.
    /// </summary>
    [Fact]
    public void ToStringUsesSeparator()
    {
        VersionedProjectionKey k = new("X", "Y", 9L);
        Assert.Equal("X|Y|9", k.ToString());
    }
}