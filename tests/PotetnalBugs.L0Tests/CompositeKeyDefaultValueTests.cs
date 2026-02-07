using System;

using Mississippi.Aqueduct.Abstractions.Keys;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates default-value invariant breaks in composite key structs.
/// </summary>
public sealed class CompositeKeyDefaultValueTests
{
    /// <summary>
    ///     default(BrookKey) bypasses constructor validation and produces null components.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "default(BrookKey) bypasses constructor null validation, producing null BrookName and EntityId. " +
        "ToString then emits a delimiter-only key that looks valid but contains no components.",
        FilePath = "src/EventSourcing.Brooks.Abstractions/BrookKey.cs",
        LineNumbers = "29,49,55,159",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultBrookKeyHasNullComponentsAndDelimiterString()
    {
        // Arrange - constructor rejects null components
        Assert.Throws<ArgumentNullException>(() => new BrookKey(null!, "entity"));
        Assert.Throws<ArgumentNullException>(() => new BrookKey("brook", null!));

        // Act
        BrookKey key = default;

        // Assert
        Assert.Null(key.BrookName);
        Assert.Null(key.EntityId);
        Assert.Equal("|", key.ToString());
    }

    /// <summary>
    ///     default(SignalRGroupKey) bypasses constructor validation and produces null components.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "default(SignalRGroupKey) bypasses constructor null validation, producing null HubName and GroupName. " +
        "ToString then emits ':' which resembles a valid key but has empty components.",
        FilePath = "src/Aqueduct.Abstractions/Keys/SignalRGroupKey.cs",
        LineNumbers = "34,54,60,115",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultSignalRGroupKeyHasNullComponentsAndDelimiterString()
    {
        // Arrange - constructor rejects null components
        Assert.Throws<ArgumentNullException>(() => new SignalRGroupKey(null!, "group"));
        Assert.Throws<ArgumentNullException>(() => new SignalRGroupKey("hub", null!));

        // Act
        SignalRGroupKey key = default;

        // Assert
        Assert.Null(key.HubName);
        Assert.Null(key.GroupName);
        Assert.Equal(":", key.ToString());
    }

    /// <summary>
    ///     default(SignalRClientKey) bypasses constructor validation and produces null components.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "default(SignalRClientKey) bypasses constructor null validation, producing null HubName and ConnectionId. " +
        "ToString then emits ':' which resembles a valid key but has empty components.",
        FilePath = "src/Aqueduct.Abstractions/Keys/SignalRClientKey.cs",
        LineNumbers = "34,54,60,115",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultSignalRClientKeyHasNullComponentsAndDelimiterString()
    {
        // Arrange - constructor rejects null components
        Assert.Throws<ArgumentNullException>(() => new SignalRClientKey(null!, "connection"));
        Assert.Throws<ArgumentNullException>(() => new SignalRClientKey("hub", null!));

        // Act
        SignalRClientKey key = default;

        // Assert
        Assert.Null(key.HubName);
        Assert.Null(key.ConnectionId);
        Assert.Equal(":", key.ToString());
    }

    /// <summary>
    ///     default(SnapshotStreamKey) bypasses constructor validation and produces null components.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "default(SnapshotStreamKey) bypasses constructor null validation, producing null components for " +
        "BrookName, SnapshotStorageName, EntityId, and ReducersHash.",
        FilePath = "src/EventSourcing.Snapshots.Abstractions/SnapshotStreamKey.cs",
        LineNumbers = "36,62,68,74,80,161",
        Severity = "Low",
        Category = "MissingValidation")]
    public void DefaultSnapshotStreamKeyHasNullComponentsAndDelimiterString()
    {
        // Arrange - constructor rejects null components
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey(null!, "SNAP", "id", "hash"));
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey("BROOK", null!, "id", "hash"));
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey("BROOK", "SNAP", null!, "hash"));
        Assert.Throws<ArgumentNullException>(() => new SnapshotStreamKey("BROOK", "SNAP", "id", null!));

        // Act
        SnapshotStreamKey key = default;

        // Assert
        Assert.Null(key.BrookName);
        Assert.Null(key.SnapshotStorageName);
        Assert.Null(key.EntityId);
        Assert.Null(key.ReducersHash);
        Assert.Equal("|||", key.ToString());
    }
}
