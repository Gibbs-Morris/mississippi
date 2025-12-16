using System;


namespace Mississippi.EventSourcing.Snapshots.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="SnapshotStreamKey" />.
/// </summary>
public class SnapshotStreamKeyTests
{
    /// <summary>
    ///     Ensures the constructor enforces maximum length.
    /// </summary>
    [Fact]
    public void ConstructorEnforcesMaxLength()
    {
        // Sum of lengths plus two separators must not exceed 2048; exceed by one to trigger.
        string projectionType = new('a', 1024);
        string projectionId = new('b', 1024);
        string reducersHash = "h";
        Assert.Throws<ArgumentException>(() => new SnapshotStreamKey(projectionType, projectionId, reducersHash));
    }

    /// <summary>
    ///     Ensures null components are rejected.
    /// </summary>
    /// <param name="projectionType">Projection type identifier.</param>
    /// <param name="projectionId">Projection instance identifier.</param>
    /// <param name="reducersHash">Reducers hash value.</param>
    [Theory]
    [InlineData(null, "id", "h")]
    [InlineData("proj", null, "h")]
    [InlineData("proj", "id", null)]
    public void ConstructorNullThrows(
        string? projectionType,
        string? projectionId,
        string? reducersHash
    )
    {
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey(
            projectionType!,
            projectionId!,
            reducersHash!));
    }

    /// <summary>
    ///     Ensures separator characters are rejected.
    /// </summary>
    [Fact]
    public void ConstructorRejectsSeparator()
    {
        Assert.Throws<ArgumentException>(() => new SnapshotStreamKey("p|q", "id", "hash"));
        Assert.Throws<ArgumentException>(() => new SnapshotStreamKey("proj", "i|d", "hash"));
        Assert.Throws<ArgumentException>(() => new SnapshotStreamKey("proj", "id", "h|ash"));
    }

    /// <summary>
    ///     Ensures the constructor stores all components.
    /// </summary>
    [Fact]
    public void ConstructorStoresComponents()
    {
        SnapshotStreamKey key = new("proj", "id", "hash");
        Assert.Equal("proj", key.ProjectionType);
        Assert.Equal("id", key.ProjectionId);
        Assert.Equal("hash", key.ReducersHash);
        Assert.Equal("proj|id|hash", key.ToString());
    }

    /// <summary>
    ///     Verifies the static conversion helper returns the composite string.
    /// </summary>
    [Fact]
    public void FromStreamKeyReturnsCompositeString()
    {
        SnapshotStreamKey key = new("type", "id", "hash");
        string composite = SnapshotStreamKey.FromStreamKey(key);
        Assert.Equal("type|id|hash", composite);
    }

    /// <summary>
    ///     Ensures malformed composites throw.
    /// </summary>
    [Fact]
    public void FromStringBadFormatThrows()
    {
        Assert.Throws<FormatException>(() => SnapshotStreamKey.FromString("too|few"));
        Assert.Throws<FormatException>(() => SnapshotStreamKey.FromString("too|many|parts|extra"));
    }

    /// <summary>
    ///     Ensures null composites throw.
    /// </summary>
    [Fact]
    public void FromStringNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotStreamKey.FromString(null!));
    }

    /// <summary>
    ///     Ensures valid composites parse correctly.
    /// </summary>
    [Fact]
    public void FromStringParsesComposite()
    {
        SnapshotStreamKey key = SnapshotStreamKey.FromString("type|id|hash");
        Assert.Equal("type", key.ProjectionType);
        Assert.Equal("id", key.ProjectionId);
        Assert.Equal("hash", key.ReducersHash);
    }

    /// <summary>
    ///     Confirms implicit conversions round-trip correctly.
    /// </summary>
    [Fact]
    public void ImplicitConversionsWork()
    {
        SnapshotStreamKey key = new("type", "id", "hash");
        string composite = key;
        Assert.Equal("type|id|hash", composite);
        SnapshotStreamKey parsed = "type|id|hash";
        Assert.Equal(key, parsed);
    }
}