namespace Mississippi.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotStorageOptions" />.
/// </summary>
public sealed class SnapshotStorageOptionsTests
{
    /// <summary>
    ///     Verifies ContainerId can be set via configuration.
    /// </summary>
    [Fact]
    public void ContainerIdShouldBeSettable()
    {
        SnapshotStorageOptions options = new()
        {
            ContainerId = "custom-container",
        };
        Assert.Equal("custom-container", options.ContainerId);
    }

    /// <summary>
    ///     Verifies ContainerId returns the default value.
    /// </summary>
    [Fact]
    public void ContainerIdShouldReturnDefaultValue()
    {
        SnapshotStorageOptions options = new();
        Assert.Equal("snapshots", options.ContainerId);
    }

    /// <summary>
    ///     Verifies DatabaseId can be set.
    /// </summary>
    [Fact]
    public void DatabaseIdShouldBeSettable()
    {
        SnapshotStorageOptions options = new()
        {
            DatabaseId = "custom-db",
        };
        Assert.Equal("custom-db", options.DatabaseId);
    }

    /// <summary>
    ///     Verifies DatabaseId returns the default value.
    /// </summary>
    [Fact]
    public void DatabaseIdShouldReturnDefaultValue()
    {
        SnapshotStorageOptions options = new();
        Assert.Equal(SnapshotCosmosDefaults.DatabaseId, options.DatabaseId);
    }

    /// <summary>
    ///     Verifies QueryBatchSize can be set.
    /// </summary>
    [Fact]
    public void QueryBatchSizeShouldBeSettable()
    {
        SnapshotStorageOptions options = new()
        {
            QueryBatchSize = 50,
        };
        Assert.Equal(50, options.QueryBatchSize);
    }

    /// <summary>
    ///     Verifies QueryBatchSize returns the default value.
    /// </summary>
    [Fact]
    public void QueryBatchSizeShouldReturnDefaultValue()
    {
        SnapshotStorageOptions options = new();
        Assert.Equal(100, options.QueryBatchSize);
    }

    /// <summary>
    ///     Verifies public snapshot Cosmos defaults constants retain expected contract values.
    /// </summary>
    [Fact]
    public void SnapshotCosmosDefaultsShouldMatchExpectedContractValues()
    {
        Assert.Equal("mississippi", SnapshotCosmosDefaults.DatabaseId);
        Assert.Equal("snapshots", SnapshotCosmosDefaults.ContainerId);
        Assert.Equal("mississippi-cosmos-snapshots", SnapshotCosmosDefaults.CosmosContainerServiceKey);
        Assert.Equal("mississippi-cosmos-snapshots-client", SnapshotCosmosDefaults.CosmosClientServiceKey);
    }
}