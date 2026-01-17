using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotStorageModel" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots Cosmos")]
[AllureSubSuite("Storage Model")]
public sealed class SnapshotStorageModelTests
{
    /// <summary>
    ///     Verifies properties can be set during initialization.
    /// </summary>
    [Fact]
    public void SnapshotStorageModelShouldStoreValues()
    {
        SnapshotStorageModel model = new()
        {
            Data = new byte[] { 1, 2 },
            DataContentType = "ct",
            StreamKey = new("brook", "t", "i", "h"),
            Version = 5,
        };
        Assert.Equal(new byte[] { 1, 2 }, model.Data);
        Assert.Equal("ct", model.DataContentType);
        Assert.Equal(new("brook", "t", "i", "h"), model.StreamKey);
        Assert.Equal(5, model.Version);
    }

    /// <summary>
    ///     Verifies defaults are empty and zeroed.
    /// </summary>
    [Fact]
    public void SnapshotStorageModelShouldUseDefaults()
    {
        SnapshotStorageModel model = new();
        Assert.Empty(model.Data);
        Assert.Equal(string.Empty, model.DataContentType);
        Assert.Equal(default, model.StreamKey);
        Assert.Equal(0, model.Version);
    }
}