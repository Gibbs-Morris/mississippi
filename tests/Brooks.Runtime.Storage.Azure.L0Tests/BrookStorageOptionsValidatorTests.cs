using Microsoft.Extensions.Options;

using Mississippi.Brooks.Runtime.Storage.Azure;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Validation tests for the Brooks Azure options surface.
/// </summary>
public sealed class BrookStorageOptionsValidatorTests
{
    private static readonly BrookStorageOptionsValidator Validator = new();

    /// <summary>
    ///     Verifies that the default Brooks Azure options pass validation.
    /// </summary>
    [Fact]
    public void ValidateReturnsSuccessForDefaultOptions()
    {
        ValidateOptionsResult result = Validator.Validate(name: null, new BrookStorageOptions());

        Assert.True(result.Succeeded);
    }

    /// <summary>
    ///     Verifies that the validator rejects event and lock containers sharing the same name.
    /// </summary>
    [Fact]
    public void ValidateFailsWhenBrooksAndLocksShareAContainer()
    {
        BrookStorageOptions options = new()
        {
            ContainerName = "shared-container",
            LockContainerName = "shared-container",
        };

        ValidateOptionsResult result = Validator.Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures!, failure => failure.Contains("different", System.StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that the shared-account snapshot container name is reserved away from the Brooks topology.
    /// </summary>
    [Fact]
    public void ValidateFailsWhenSnapshotsContainerNameLeaksIntoBrooksTopology()
    {
        BrookStorageOptions options = new()
        {
            LockContainerName = BrookAzureDefaults.SnapshotContainerName,
        };

        ValidateOptionsResult result = Validator.Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains(BrookAzureDefaults.SnapshotContainerName, System.StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that Azure-invalid container names are rejected before any network calls happen.
    /// </summary>
    [Fact]
    public void ValidateFailsWhenContainerNameIsInvalid()
    {
        BrookStorageOptions options = new()
        {
            ContainerName = "Invalid--Name",
        };

        ValidateOptionsResult result = Validator.Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains(nameof(BrookStorageOptions.ContainerName), System.StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies invalid lock container names are rejected before any network calls happen.
    /// </summary>
    [Fact]
    public void ValidateFailsWhenLockContainerNameIsInvalid()
    {
        BrookStorageOptions options = new()
        {
            LockContainerName = "Invalid--Lock",
        };

        ValidateOptionsResult result = Validator.Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains(nameof(BrookStorageOptions.LockContainerName), System.StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies the Brooks event container cannot reuse the reserved shared-account snapshots container name.
    /// </summary>
    [Fact]
    public void ValidateFailsWhenBrooksContainerUsesReservedSnapshotsName()
    {
        BrookStorageOptions options = new()
        {
            ContainerName = BrookAzureDefaults.SnapshotContainerName,
        };

        ValidateOptionsResult result = Validator.Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains(nameof(BrookStorageOptions.ContainerName), System.StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies the keyed blob service registration key must be configured.
    /// </summary>
    [Fact]
    public void ValidateFailsWhenBlobServiceClientServiceKeyIsBlank()
    {
        BrookStorageOptions options = new()
        {
            BlobServiceClientServiceKey = string.Empty,
        };

        ValidateOptionsResult result = Validator.Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains(nameof(BrookStorageOptions.BlobServiceClientServiceKey), System.StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies lease renewal must happen before the configured lease duration expires.
    /// </summary>
    [Fact]
    public void ValidateFailsWhenLeaseRenewalThresholdIsNotLessThanLeaseDuration()
    {
        BrookStorageOptions options = new()
        {
            LeaseDurationSeconds = 30,
            LeaseRenewalThresholdSeconds = 30,
        };

        ValidateOptionsResult result = Validator.Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures!, failure => failure.Contains("less than LeaseDurationSeconds", System.StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies batch and prefetch knobs reject non-positive values.
    /// </summary>
    [Fact]
    public void ValidateFailsWhenReadOrBatchSettingsAreNonPositive()
    {
        BrookStorageOptions options = new()
        {
            MaxEventsPerBatch = 0,
            ReadPrefetchCount = 0,
        };

        ValidateOptionsResult result = Validator.Validate(name: null, options);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures!, failure => failure.Contains(nameof(BrookStorageOptions.MaxEventsPerBatch), System.StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains(nameof(BrookStorageOptions.ReadPrefetchCount), System.StringComparison.Ordinal));
    }
}