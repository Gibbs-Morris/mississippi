using Mississippi.Core.Abstractions.Cqrs;


namespace Mississippi.Core.Abstractions.Tests.Cqrs;

/// <summary>
///     Tests for query reference related types.
/// </summary>
public class QueryReferenceTests
{
    /// <summary>
    ///     Ensures that the <see cref="QueryReference" /> constructor stores values correctly.
    /// </summary>
    [Fact]
    public void QueryReferenceStoresValues()
    {
        QueryReference reference = new("order", "123");
        Assert.Equal("order", reference.QueryType);
        Assert.Equal("123", reference.Id);
        Assert.Equal("order/123", reference.Path);
    }

    /// <summary>
    ///     Ensures that the <see cref="VersionedQueryReference" /> constructor stores values correctly.
    /// </summary>
    [Fact]
    public void VersionedQueryReferenceStoresValues()
    {
        VersionedQueryReference reference = new("order", "123", 5);
        Assert.Equal("order", reference.QueryType);
        Assert.Equal("123", reference.Id);
        Assert.Equal(5, reference.Version);
        Assert.Equal("order/123/5", reference.VersionedPath);
    }

    /// <summary>
    ///     Verifies that an invalid token causes an <see cref="ArgumentException" />.
    /// </summary>
    [Fact]
    public void QueryReferenceInvalidTokenThrows()
    {
        Assert.Throws<ArgumentException>(() => new QueryReference("invalid*", "id"));
    }

    /// <summary>
    ///     Verifies that a version of zero causes an <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [Fact]
    public void VersionedQueryReferenceInvalidVersionThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VersionedQueryReference("type", "id", 0));
    }

    /// <summary>
    ///     Verifies that a null token causes an <see cref="ArgumentNullException" />.
    /// </summary>
    [Fact]
    public void QueryReferenceNullTokenThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new QueryReference(null!, "id"));
    }

    /// <summary>
    ///     Verifies that whitespace tokens cause an <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="token">The whitespace token.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void QueryReferenceWhitespaceTokenThrows(
        string token
    )
    {
        Assert.Throws<ArgumentException>(() => new QueryReference(token, "id"));
    }

    /// <summary>
    ///     Verifies that negative versions are allowed.
    /// </summary>
    [Fact]
    public void VersionedQueryReferenceAllowsNegativeVersion()
    {
        VersionedQueryReference reference = new("type", "id", -1);
        Assert.Equal(-1, reference.Version);
    }
}