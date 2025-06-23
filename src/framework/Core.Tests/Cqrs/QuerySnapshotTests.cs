using Mississippi.Core.Abstractions.Cqrs;

namespace Mississippi.Core.Tests.Cqrs;

/// <summary>
/// Tests for <see cref="QuerySnapshot{TState}"/>.
/// </summary>
public class QuerySnapshotTests
{
    /// <summary>
    /// Verifies that the constructor sets all properties as expected.
    /// </summary>
    [Fact]
    public void ConstructorSetsProperties()
    {
        VersionedQueryReference reference = new("type", "id", 1);
        QuerySnapshot<string> snapshot = new(reference, "state");
        Assert.Equal(reference, snapshot.Reference);
        Assert.Equal("state", snapshot.State);
    }

    /// <summary>
    /// Ensures an <see cref="ArgumentException"/> is thrown when the reference is default.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenReferenceDefault()
    {
        Assert.Throws<ArgumentException>(() => new QuerySnapshot<string>(default, "state"));
    }

    /// <summary>
    /// Ensures an <see cref="ArgumentNullException"/> is thrown when the state is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenStateNull()
    {
        VersionedQueryReference reference = new("type", "id", 1);
        Assert.Throws<ArgumentNullException>(() => new QuerySnapshot<string>(reference, null));
    }
}
