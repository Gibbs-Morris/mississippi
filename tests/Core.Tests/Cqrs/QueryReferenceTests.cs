using Mississippi.Core.Abstractions.Cqrs.Query;


namespace Mississippi.Core.Abstractions.Tests.Cqrs;

/// <summary>
///     Tests for <see cref="QueryReference" />.
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
    ///     Verifies that an invalid token causes an <see cref="ArgumentException" />.
    /// </summary>
    [Fact]
    public void QueryReferenceInvalidTokenThrows()
    {
        Assert.Throws<ArgumentException>(() => new QueryReference("invalid*", "id"));
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
}