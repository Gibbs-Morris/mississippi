using Mississippi.Core.Abstractions.Cqrs;


namespace Mississippi.Core.Abstractions.Tests.Cqrs;

/// <summary>
///     Tests for <see cref="VersionedQueryReference" />.
/// </summary>
public class VersionedQueryReferenceTests
{
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
    ///     Verifies that a version of zero causes an <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [Fact]
    public void VersionedQueryReferenceInvalidVersionThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VersionedQueryReference("type", "id", 0));
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

    /// <summary>
    ///     Verifies that an invalid token causes an <see cref="ArgumentException" />.
    /// </summary>
    [Fact]
    public void VersionedQueryReferenceInvalidTokenThrows()
    {
        Assert.Throws<ArgumentException>(() => new VersionedQueryReference("invalid*", "id", 1));
    }

    /// <summary>
    ///     Verifies that a null token causes an <see cref="ArgumentNullException" />.
    /// </summary>
    [Fact]
    public void VersionedQueryReferenceNullTokenThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new VersionedQueryReference(null!, "id", 1));
    }

    /// <summary>
    ///     Verifies that whitespace tokens cause an <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="token">The whitespace token.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void VersionedQueryReferenceWhitespaceTokenThrows(
        string token
    )
    {
        Assert.Throws<ArgumentException>(() => new VersionedQueryReference(token, "id", 1));
    }
}