namespace Mississippi.EventSourcing.Cosmos.Tests;

/// <summary>
///     Tests for <see cref="BrookStorageOptions" /> defaults and mutability.
/// </summary>
public class BrookStorageOptionsTests
{
    /// <summary>
    ///     Verifies default values are sensible.
    /// </summary>
    [Fact]
    public void DefaultsShouldBeSensible()
    {
        BrookStorageOptions options = new();
        Assert.Equal("mississippi", options.DatabaseId);
        Assert.Equal("brooks", options.ContainerId);
        Assert.Equal("brook-locks", options.LockContainerName);
        Assert.Equal(100, options.QueryBatchSize);
        Assert.Equal(90, options.MaxEventsPerBatch);
        Assert.Equal(60, options.LeaseDurationSeconds);
        Assert.Equal(1_700_000, options.MaxRequestSizeBytes);
        Assert.Equal(20, options.LeaseRenewalThresholdSeconds);
    }

    /// <summary>
    ///     Verifies settable properties can be changed.
    /// </summary>
    [Fact]
    public void PropertiesShouldBeSettable()
    {
        BrookStorageOptions options = new()
        {
            DatabaseId = "db",
            LockContainerName = "locks",
            QueryBatchSize = 10,
            MaxEventsPerBatch = 50,
            LeaseDurationSeconds = 120,
            MaxRequestSizeBytes = 1_000_000,
            LeaseRenewalThresholdSeconds = 15,
        };
        Assert.Equal("db", options.DatabaseId);
        Assert.Equal("locks", options.LockContainerName);
        Assert.Equal(10, options.QueryBatchSize);
        Assert.Equal(50, options.MaxEventsPerBatch);
        Assert.Equal(120, options.LeaseDurationSeconds);
        Assert.Equal(1_000_000, options.MaxRequestSizeBytes);
        Assert.Equal(15, options.LeaseRenewalThresholdSeconds);
        Assert.Equal("brooks", options.ContainerId); // get-only default
    }
}