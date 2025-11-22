using System;

using Mississippi.Projections.Projections;


namespace Mississippi.Projections.Tests;

/// <summary>
///     Tests for <see cref="ProjectionKey" /> behavior.
/// </summary>
public class ProjectionKeyTests
{
    /// <summary>
    ///     Ensures constructor rejects components containing separators.
    /// </summary>
    [Fact]
    public void ComponentWithSeparatorThrows()
    {
        Assert.Throws<ArgumentException>(() => new ProjectionKey("a|b", "id"));
        Assert.Throws<ArgumentException>(() => new ProjectionKey("path", "i|d"));
    }

    /// <summary>
    ///     Ensures the constructor accepts components at the exact limits.
    /// </summary>
    [Fact]
    public void ConstructorAllowsExactMaxLength()
    {
        string path = new('x', 512);
        string id = new('y', 511);
        ProjectionKey key = new(path, id);
        Assert.Equal(path, key.Path);
        Assert.Equal(id, key.Id);
    }

    /// <summary>
    ///     Ensures a well-formed instance produces the expected text representation.
    /// </summary>
    [Fact]
    public void ConstructorCreatesKey()
    {
        ProjectionKey key = new("path", "id");
        Assert.Equal("path", key.Path);
        Assert.Equal("id", key.Id);
        Assert.Equal("path|id", key.ToString());
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
        Assert.Throws<ArgumentNullException>(() => new ProjectionKey(path!, id!));
    }

    /// <summary>
    ///     Ensures components that exceed max length by one are rejected.
    /// </summary>
    [Fact]
    public void ConstructorRejectsMaxLengthPlusOne()
    {
        string path = new('x', 512);
        string id = new('y', 512);
        Assert.Throws<ArgumentException>(() => new ProjectionKey(path, id));
    }

    /// <summary>
    ///     Ensures the combined length constraint is enforced.
    /// </summary>
    [Fact]
    public void ConstructorWhenCombinedLengthTooLongThrows()
    {
        string longPath = new('x', 1024);
        Assert.Throws<ArgumentException>(() => new ProjectionKey(longPath, string.Empty));
    }

    /// <summary>
    ///     Ensures the formatting helper emits the expected string.
    /// </summary>
    [Fact]
    public void FromProjectionKeyReturnsStringRepresentation()
    {
        ProjectionKey k = new("A", "B");
        string s = ProjectionKey.FromProjectionKey(k);
        Assert.Equal("A|B", s);
    }

    /// <summary>
    ///     Ensures parsing supports empty path components.
    /// </summary>
    [Fact]
    public void FromStringAllowsEmptyPath()
    {
        ProjectionKey key = ProjectionKey.FromString("|identifier");
        Assert.Equal(string.Empty, key.Path);
        Assert.Equal("identifier", key.Id);
    }

    /// <summary>
    ///     Ensures invalid text is rejected.
    /// </summary>
    [Fact]
    public void FromStringBadFormatThrows()
    {
        Assert.Throws<FormatException>(() => ProjectionKey.FromString("no-separator"));
    }

    /// <summary>
    ///     Ensures null text input is not allowed.
    /// </summary>
    [Fact]
    public void FromStringWhenNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ProjectionKey.FromString(null!));
    }

    /// <summary>
    ///     Ensures implicit conversions round-trip correctly.
    /// </summary>
    [Fact]
    public void ImplicitConversionsWork()
    {
        ProjectionKey k = new("p", "i");
        string s = k;
        Assert.Equal("p|i", s);
        ProjectionKey parsed = "p|i";
        Assert.Equal("p", parsed.Path);
        Assert.Equal("i", parsed.Id);
    }

    /// <summary>
    ///     Ensures the string conversion uses the separator.
    /// </summary>
    [Fact]
    public void ToStringUsesSeparator()
    {
        ProjectionKey k = new("X", "Y");
        Assert.Equal("X|Y", k.ToString());
    }
}