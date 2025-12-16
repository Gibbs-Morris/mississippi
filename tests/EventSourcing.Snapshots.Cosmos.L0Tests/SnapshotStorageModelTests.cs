using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotStorageModel" />.
/// </summary>
public sealed class SnapshotStorageModelTests
{
    /// <summary>
    ///     Verifies defaults are empty and zeroed.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void SnapshotStorageModelShouldUseDefaults()
    {
        SnapshotStorageModel model = new();
        Assert.Empty(model.Data);
        Assert.Equal(string.Empty, model.DataContentType);
        Assert.Equal(default, model.StreamKey);
        Assert.Equal(0, model.Version);
    }

    /// <summary>
    ///     Verifies properties can be set during initialization.
    /// </summary>
    [AllureEpic("Snapshots")]
    [Fact]
    public void SnapshotStorageModelShouldStoreValues()
    {
        SnapshotStorageModel model = new()
        {
            Data = new byte[] { 1, 2 },
            DataContentType = "ct",
            StreamKey = new SnapshotStreamKey("t", "i", "h"),
            Version = 5,
        };
        Assert.Equal(new byte[] { 1, 2 }, model.Data);
        Assert.Equal("ct", model.DataContentType);
        Assert.Equal(new SnapshotStreamKey("t", "i", "h"), model.StreamKey);
        Assert.Equal(5, model.Version);
    }
}
