using System;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Snapshots.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="SnapshotKey" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots Abstractions")]
[AllureSubSuite("Snapshot Key")]
public sealed class SnapshotKeyTests
{
    /// <summary>
    ///     Ensures negative versions are rejected.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNegativeVersion()
    {
        SnapshotStreamKey stream = new("TEST.BROOK", "proj", "id", "hash");
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnapshotKey(stream, -1));
    }

    /// <summary>
    ///     Ensures the constructor stores the provided values.
    /// </summary>
    [Fact]
    public void ConstructorStoresValues()
    {
        SnapshotStreamKey stream = new("TEST.BROOK", "proj", "id", "hash");
        SnapshotKey key = new(stream, 42);
        Assert.Equal(stream, key.Stream);
        Assert.Equal(42, key.Version);
        Assert.Equal("TEST.BROOK|id|42|proj|hash", key.ToString());
    }

    /// <summary>
    ///     Verifies the static conversion helper returns the composite string.
    /// </summary>
    [Fact]
    public void FromSnapshotKeyReturnsCompositeString()
    {
        SnapshotStreamKey stream = new("TEST.BROOK", "type", "id", "hash");
        SnapshotKey key = new(stream, 9);
        string composite = SnapshotKey.FromSnapshotKey(key);
        Assert.Equal("TEST.BROOK|id|9|type|hash", composite);
    }

    /// <summary>
    ///     Ensures malformed composite strings throw.
    /// </summary>
    [Fact]
    public void FromStringBadFormatThrows()
    {
        Assert.Throws<FormatException>(() => SnapshotKey.FromString("too|few|parts|four"));
        Assert.Throws<FormatException>(() => SnapshotKey.FromString("brook|id|notanumber|type|hash"));
    }

    /// <summary>
    ///     Ensures null composite strings throw.
    /// </summary>
    [Fact]
    public void FromStringNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotKey.FromString(null!));
    }

    /// <summary>
    ///     Ensures valid composite strings parse correctly.
    /// </summary>
    [Fact]
    public void FromStringParsesComposite()
    {
        SnapshotKey key = SnapshotKey.FromString("brook|id|7|type|hash");
        Assert.Equal("brook", key.Stream.BrookName);
        Assert.Equal("type", key.Stream.SnapshotStorageName);
        Assert.Equal("id", key.Stream.EntityId);
        Assert.Equal("hash", key.Stream.ReducersHash);
        Assert.Equal(7, key.Version);
    }

    /// <summary>
    ///     Ensures composite strings with extra segments throw.
    /// </summary>
    [Fact]
    public void FromStringTooManyPartsThrows()
    {
        Assert.Throws<FormatException>(() => SnapshotKey.FromString("brook|id|1|type|hash|extra"));
    }

    /// <summary>
    ///     Confirms implicit conversions round-trip correctly.
    /// </summary>
    [Fact]
    public void ImplicitConversionsWork()
    {
        SnapshotStreamKey stream = new("TEST.BROOK", "type", "id", "hash");
        SnapshotKey key = new(stream, 5);
        string composite = key;
        Assert.Equal("TEST.BROOK|id|5|type|hash", composite);
        SnapshotKey parsed = "TEST.BROOK|id|5|type|hash";
        Assert.Equal(key, parsed);
    }
}