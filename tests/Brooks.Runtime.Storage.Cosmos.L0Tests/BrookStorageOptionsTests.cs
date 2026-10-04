using System.Collections.Generic;
using System.Linq;

using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Hosting.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="BrookStorageOptions" /> defaults and mutability.
/// </summary>
public sealed class BrookStorageOptionsTests
{
    /// <summary>
    ///     Verifies public Brooks Cosmos defaults constants retain expected contract values.
    /// </summary>
    [Fact]
    public void BrookCosmosDefaultsShouldMatchExpectedContractValues()
    {
        Assert.Equal("mississippi", BrookCosmosDefaults.DatabaseId);
        Assert.Equal("brooks", BrookCosmosDefaults.ContainerId);
        Assert.Equal("locks", BrookCosmosDefaults.LockContainerId);
        Assert.Equal("mississippi-cosmos-brooks", BrookCosmosDefaults.CosmosContainerServiceKey);
        Assert.Equal("mississippi-cosmos-brooks-client", BrookCosmosDefaults.CosmosClientServiceKey);
        Assert.Equal("mississippi-blob-locking", BrookCosmosDefaults.BlobLockingServiceKey);
    }

    /// <summary>
    ///     Verifies that the nested builder snapshots every provider option before registration.
    /// </summary>
    [Fact]
    public void BuilderSnapshotsAllProviderOptions()
    {
        CosmosBrookStorageBuilder builder = new();
        builder.ContainerId = "events";
        builder.CosmosClientServiceKey = "cosmos";
        builder.DatabaseId = "database";
        builder.LeaseDurationSeconds = 30;
        builder.LeaseRenewalThresholdSeconds = 10;
        builder.LockContainerName = "locks";
        builder.MaxEventsPerBatch = 25;
        builder.MaxRequestSizeBytes = 10_000;
        builder.QueryBatchSize = -1;
        BrookStorageOptions snapshot = builder.Snapshot();
        builder.DatabaseId = "changed";
        Assert.Equal("events", snapshot.ContainerId);
        Assert.Equal("cosmos", snapshot.CosmosClientServiceKey);
        Assert.Equal("database", snapshot.DatabaseId);
        Assert.Equal(30, snapshot.LeaseDurationSeconds);
        Assert.Equal(10, snapshot.LeaseRenewalThresholdSeconds);
        Assert.Equal("locks", snapshot.LockContainerName);
        Assert.Equal(25, snapshot.MaxEventsPerBatch);
        Assert.Equal(10_000, snapshot.MaxRequestSizeBytes);
        Assert.Equal(-1, snapshot.QueryBatchSize);
        Assert.Empty(builder.Validate());
    }

    /// <summary>Verifies closed nested scopes return their stable diagnostic and reject mutation.</summary>
    [Fact]
    public void ClosedScopeRejectsConfiguration()
    {
        CosmosBrookStorageBuilder builder = new();
        builder.Close();
        Assert.Equal(
            BrookStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
            Assert.Single(builder.Validate()).Code);
        Assert.Throws<BuilderValidationException>(() => builder.DatabaseId = "late");
    }

    /// <summary>
    ///     Verifies default values are sensible.
    /// </summary>
    [Fact]
    public void DefaultsShouldBeSensible()
    {
        BrookStorageOptions options = new();
        Assert.Equal(BrookCosmosDefaults.DatabaseId, options.DatabaseId);
        Assert.Equal(BrookCosmosDefaults.ContainerId, options.ContainerId);
        Assert.Equal(BrookCosmosDefaults.LockContainerId, options.LockContainerName);
        Assert.Equal(100, options.QueryBatchSize);
        Assert.Equal(90, options.MaxEventsPerBatch);
        Assert.Equal(60, options.LeaseDurationSeconds);
        Assert.Equal(1_700_000, options.MaxRequestSizeBytes);
        Assert.Equal(20, options.LeaseRenewalThresholdSeconds);
    }

    /// <summary>
    ///     Verifies that each material option failure has a distinct stable diagnostic code.
    /// </summary>
    [Fact]
    public void InvalidOptionsProduceDistinctDiagnostics()
    {
        CosmosBrookStorageBuilder builder = new()
        {
            ContainerId = " ",
            CosmosClientServiceKey = string.Empty,
            DatabaseId = " ",
            LeaseDurationSeconds = 14,
            LeaseRenewalThresholdSeconds = -1,
            LockContainerName = string.Empty,
            MaxEventsPerBatch = 0,
            MaxRequestSizeBytes = BatchSizeEstimator.BatchOverheadBytes,
            QueryBatchSize = 0,
        };
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        Assert.Equal(9, diagnostics.Count);
        Assert.Equal(9, diagnostics.Select(diagnostic => diagnostic.Code).Distinct().Count());
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.ContainerIdRequired);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.CosmosClientServiceKeyRequired);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.DatabaseIdRequired);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.LockContainerNameRequired);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.QueryBatchSizeInvalid);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.LeaseDurationInvalid);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.LeaseRenewalThresholdInvalid);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.MaxEventsPerBatchInvalid);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.MaxRequestSizeInvalid);
    }

    /// <summary>Verifies the finite Blob lease duration boundaries.</summary>
    /// <param name="duration">The configured lease duration.</param>
    /// <param name="valid">Whether the duration is supported.</param>
    [Theory]
    [InlineData(15, true)]
    [InlineData(60, true)]
    [InlineData(14, false)]
    [InlineData(61, false)]
    public void LeaseDurationUsesFiniteSupportedRange(
        int duration,
        bool valid
    )
    {
        CosmosBrookStorageBuilder builder = new()
        {
            LeaseDurationSeconds = duration,
            LeaseRenewalThresholdSeconds = 0,
        };
        bool hasDiagnostic = builder.Validate()
            .Any(diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.LeaseDurationInvalid);
        Assert.Equal(!valid, hasDiagnostic);
    }

    /// <summary>Verifies lease renewal thresholds stay below their finite duration.</summary>
    /// <param name="threshold">The configured renewal threshold.</param>
    /// <param name="valid">Whether the threshold is supported.</param>
    [Theory]
    [InlineData(0, true)]
    [InlineData(59, true)]
    [InlineData(-1, false)]
    [InlineData(60, false)]
    public void LeaseRenewalThresholdMustBeBelowDuration(
        int threshold,
        bool valid
    )
    {
        CosmosBrookStorageBuilder builder = new()
        {
            LeaseDurationSeconds = 60,
            LeaseRenewalThresholdSeconds = threshold,
        };
        bool hasDiagnostic = builder.Validate()
            .Any(diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.LeaseRenewalThresholdInvalid);
        Assert.Equal(!valid, hasDiagnostic);
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
            LeaseDurationSeconds = 30,
            MaxRequestSizeBytes = 1_000_000,
            LeaseRenewalThresholdSeconds = 15,
        };
        Assert.Equal("db", options.DatabaseId);
        Assert.Equal("locks", options.LockContainerName);
        Assert.Equal(10, options.QueryBatchSize);
        Assert.Equal(50, options.MaxEventsPerBatch);
        Assert.Equal(30, options.LeaseDurationSeconds);
        Assert.Equal(1_000_000, options.MaxRequestSizeBytes);
        Assert.Equal(15, options.LeaseRenewalThresholdSeconds);
        Assert.Equal(
            BrookCosmosDefaults.ContainerId,
            options.ContainerId); // default retained because ContainerId was not assigned
    }

    /// <summary>Verifies the supported positive and dynamic query batch sizes.</summary>
    /// <param name="queryBatchSize">The configured query page size.</param>
    /// <param name="valid">Whether the value is supported.</param>
    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-2, false)]
    public void QueryBatchSizeSupportsPositiveOrDynamicValues(
        int queryBatchSize,
        bool valid
    )
    {
        CosmosBrookStorageBuilder builder = new()
        {
            QueryBatchSize = queryBatchSize,
        };
        bool hasDiagnostic = builder.Validate()
            .Any(diagnostic => diagnostic.Code == BrookStorageBuilderDiagnosticCodes.QueryBatchSizeInvalid);
        Assert.Equal(!valid, hasDiagnostic);
    }
}