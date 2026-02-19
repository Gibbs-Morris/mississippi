using System;

using Mississippi.Aqueduct.Abstractions.Keys;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that default-value invariant breaks in composite key structs are now fixed
///     via C# 14 field keyword null-coalescing in property getters.
/// </summary>
public sealed class CompositeKeyDefaultValueTests
{
    /// <summary>
    ///     FIXED: default(BrookKey) now returns non-null components via C# 14 field keyword.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: default(BrookKey) previously produced null BrookName and EntityId. " +
        "C# 14 field keyword null-coalescing now ensures components are string.Empty.",
        FilePath = "src/EventSourcing.Brooks.Abstractions/BrookKey.cs",
        LineNumbers = "29,49,55,159",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultBrookKeyHasNonNullComponentsAndDelimiterString()
    {
        // Arrange - constructor rejects null components
        Assert.Throws<ArgumentNullException>(() => new BrookKey(null!, "entity"));
        Assert.Throws<ArgumentNullException>(() => new BrookKey("brook", null!));

        // Act
        BrookKey key = default;

        // Assert - components are now string.Empty, not null
        Assert.Equal(string.Empty, key.BrookName);
        Assert.Equal(string.Empty, key.EntityId);
        Assert.Equal("|", key.ToString());
    }

    /// <summary>
    ///     FIXED: default(SignalRGroupKey) now returns non-null components via C# 14 field keyword.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: default(SignalRGroupKey) previously produced null HubName and GroupName. " +
        "C# 14 field keyword null-coalescing now ensures components are string.Empty.",
        FilePath = "src/Aqueduct.Abstractions/Keys/SignalRGroupKey.cs",
        LineNumbers = "34,54,60,115",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultSignalRGroupKeyHasNonNullComponentsAndDelimiterString()
    {
        // Arrange - constructor rejects null components
        Assert.Throws<ArgumentNullException>(() => new SignalRGroupKey(null!, "group"));
        Assert.Throws<ArgumentNullException>(() => new SignalRGroupKey("hub", null!));

        // Act
        SignalRGroupKey key = default;

        // Assert - components are now string.Empty, not null
        Assert.Equal(string.Empty, key.HubName);
        Assert.Equal(string.Empty, key.GroupName);
        Assert.Equal(":", key.ToString());
    }

    /// <summary>
    ///     FIXED: default(SignalRClientKey) now returns non-null components via C# 14 field keyword.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: default(SignalRClientKey) previously produced null HubName and ConnectionId. " +
        "C# 14 field keyword null-coalescing now ensures components are string.Empty.",
        FilePath = "src/Aqueduct.Abstractions/Keys/SignalRClientKey.cs",
        LineNumbers = "34,54,60,115",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultSignalRClientKeyHasNonNullComponentsAndDelimiterString()
    {
        // Arrange - constructor rejects null components
        Assert.Throws<ArgumentNullException>(() => new SignalRClientKey(null!, "connection"));
        Assert.Throws<ArgumentNullException>(() => new SignalRClientKey("hub", null!));

        // Act
        SignalRClientKey key = default;

        // Assert - components are now string.Empty, not null
        Assert.Equal(string.Empty, key.HubName);
        Assert.Equal(string.Empty, key.ConnectionId);
        Assert.Equal(":", key.ToString());
    }

    /// <summary>
    ///     FIXED: default(SnapshotStreamKey) now returns non-null components via C# 14 field keyword.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: default(SnapshotStreamKey) previously produced null components for " +
        "BrookName, SnapshotStorageName, EntityId, and ReducersHash. " +
        "C# 14 field keyword null-coalescing now ensures all components are string.Empty.",
        FilePath = "src/EventSourcing.Snapshots.Abstractions/SnapshotStreamKey.cs",
        LineNumbers = "36,62,68,74,80,161",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultSnapshotStreamKeyHasNonNullComponentsAndDelimiterString()
    {
        // Arrange - constructor rejects null components
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey(null!, "SNAP", "id", "hash"));
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey("BROOK", null!, "id", "hash"));
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey("BROOK", "SNAP", null!, "hash"));
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey("BROOK", "SNAP", "id", null!));

        // Act
        SnapshotStreamKey key = default;

        // Assert - all components are now string.Empty, not null
        Assert.Equal(string.Empty, key.BrookName);
        Assert.Equal(string.Empty, key.SnapshotStorageName);
        Assert.Equal(string.Empty, key.EntityId);
        Assert.Equal(string.Empty, key.ReducersHash);
        Assert.Equal("|||", key.ToString());
    }
}
