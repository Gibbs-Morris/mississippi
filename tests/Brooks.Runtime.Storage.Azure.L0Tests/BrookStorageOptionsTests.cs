namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Tests for Brooks Azure defaults and mutable option values.
/// </summary>
public sealed class BrookStorageOptionsTests
{
    /// <summary>
    ///     Verifies the Brooks Azure default constants remain stable.
    /// </summary>
    [Fact]
    public void BrookAzureDefaultsShouldMatchExpectedContractValues()
    {
        Assert.Equal("mississippi-blob-brooks", BrookAzureDefaults.BlobServiceClientServiceKey);
        Assert.Equal("brooks", BrookAzureDefaults.ContainerName);
        Assert.Equal("locks", BrookAzureDefaults.LockContainerName);
        Assert.Equal("snapshots", BrookAzureDefaults.SnapshotContainerName);
    }

    /// <summary>
    ///     Verifies the Brooks Azure options default posture is sensible for Increment 1.
    /// </summary>
    [Fact]
    public void DefaultsShouldBeSensible()
    {
        BrookStorageOptions options = new();

        Assert.Equal(BrookAzureDefaults.BlobServiceClientServiceKey, options.BlobServiceClientServiceKey);
        Assert.Equal(BrookAzureDefaults.ContainerName, options.ContainerName);
        Assert.Equal(BrookAzureDefaults.LockContainerName, options.LockContainerName);
        Assert.Equal(BrookStorageInitializationMode.ValidateOrCreate, options.InitializationMode);
        Assert.Equal(60, options.LeaseDurationSeconds);
        Assert.Equal(20, options.LeaseRenewalThresholdSeconds);
        Assert.Equal(90, options.MaxEventsPerBatch);
        Assert.Equal(16, options.ReadPrefetchCount);
    }

    /// <summary>
    ///     Verifies all Brooks Azure option properties can be explicitly set.
    /// </summary>
    [Fact]
    public void PropertiesShouldBeSettable()
    {
        BrookStorageOptions options = new()
        {
            BlobServiceClientServiceKey = "shared-account",
            ContainerName = "brooks-prod",
            LockContainerName = "locks-prod",
            InitializationMode = BrookStorageInitializationMode.ValidateOnly,
            LeaseDurationSeconds = 90,
            LeaseRenewalThresholdSeconds = 30,
            MaxEventsPerBatch = 75,
            ReadPrefetchCount = 8,
        };

        Assert.Equal("shared-account", options.BlobServiceClientServiceKey);
        Assert.Equal("brooks-prod", options.ContainerName);
        Assert.Equal("locks-prod", options.LockContainerName);
        Assert.Equal(BrookStorageInitializationMode.ValidateOnly, options.InitializationMode);
        Assert.Equal(90, options.LeaseDurationSeconds);
        Assert.Equal(30, options.LeaseRenewalThresholdSeconds);
        Assert.Equal(75, options.MaxEventsPerBatch);
        Assert.Equal(8, options.ReadPrefetchCount);
    }
}