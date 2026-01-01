using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Snapshots.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="SnapshotStreamKey" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots Abstractions")]
[AllureSubSuite("Snapshot Stream Key")]
public sealed class SnapshotStreamKeyTests
{
    /// <summary>
    ///     Ensures the constructor enforces maximum length.
    /// </summary>
    [Fact]
    public void ConstructorEnforcesMaxLength()
    {
        // Sum of lengths plus three separators must not exceed 2048; exceed by one to trigger.
        const string brookName = "brook";
        string projectionType = new('a', 1024);
        string projectionId = new('b', 1024);
        const string reducersHash = "h";
        Assert.Throws<ArgumentException>(() => new SnapshotStreamKey(
            brookName,
            projectionType,
            projectionId,
            reducersHash));
    }

    /// <summary>
    ///     Ensures null components are rejected.
    /// </summary>
    /// <param name="brookName">Brook name identifier.</param>
    /// <param name="projectionType">Projection type identifier.</param>
    /// <param name="projectionId">Projection instance identifier.</param>
    /// <param name="reducersHash">Reducers hash value.</param>
    [Theory]
    [InlineData(null, "proj", "id", "h")]
    [InlineData("brook", null, "id", "h")]
    [InlineData("brook", "proj", null, "h")]
    [InlineData("brook", "proj", "id", null)]
    public void ConstructorNullThrows(
        string? brookName,
        string? projectionType,
        string? projectionId,
        string? reducersHash
    )
    {
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey(
            brookName!,
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
        Assert.Throws<ArgumentException>(() => new SnapshotStreamKey("br|ook", "proj", "id", "hash"));
        Assert.Throws<ArgumentException>(() => new SnapshotStreamKey("brook", "p|roj", "id", "hash"));
        Assert.Throws<ArgumentException>(() => new SnapshotStreamKey("brook", "proj", "i|d", "hash"));
        Assert.Throws<ArgumentException>(() => new SnapshotStreamKey("brook", "proj", "id", "h|ash"));
    }

    /// <summary>
    ///     Ensures the constructor stores all components.
    /// </summary>
    [Fact]
    public void ConstructorStoresComponents()
    {
        SnapshotStreamKey key = new("TEST.BROOK", "proj", "id", "hash");
        Assert.Equal("TEST.BROOK", key.BrookName);
        Assert.Equal("proj", key.ProjectionType);
        Assert.Equal("id", key.ProjectionId);
        Assert.Equal("hash", key.ReducersHash);
        Assert.Equal("TEST.BROOK|proj|id|hash", key.ToString());
    }

    /// <summary>
    ///     Verifies the static conversion helper returns the composite string.
    /// </summary>
    [Fact]
    public void FromStreamKeyReturnsCompositeString()
    {
        SnapshotStreamKey key = new("TEST.BROOK", "type", "id", "hash");
        string composite = SnapshotStreamKey.FromStreamKey(key);
        Assert.Equal("TEST.BROOK|type|id|hash", composite);
    }

    /// <summary>
    ///     Ensures malformed composites throw.
    /// </summary>
    [Fact]
    public void FromStringBadFormatThrows()
    {
        Assert.Throws<FormatException>(() => SnapshotStreamKey.FromString("too|few|parts"));
        Assert.Throws<FormatException>(() => SnapshotStreamKey.FromString("too|many|parts|extra|five"));
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
        SnapshotStreamKey key = SnapshotStreamKey.FromString("brook|type|id|hash");
        Assert.Equal("brook", key.BrookName);
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
        SnapshotStreamKey key = new("TEST.BROOK", "type", "id", "hash");
        string composite = key;
        Assert.Equal("TEST.BROOK|type|id|hash", composite);
        SnapshotStreamKey parsed = "TEST.BROOK|type|id|hash";
        Assert.Equal(key, parsed);
    }
}