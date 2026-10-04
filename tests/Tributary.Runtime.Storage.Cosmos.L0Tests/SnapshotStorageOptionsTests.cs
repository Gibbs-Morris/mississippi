using System.Collections.Generic;
using System.Linq;

using Mississippi.Hosting.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests for Snapshot Cosmos options and nested builder behavior.
/// </summary>
public sealed class SnapshotStorageOptionsTests
{
    /// <summary>
    ///     Verifies the nested builder snapshots all four provider options before registration.
    /// </summary>
    [Fact]
    public void BuilderSnapshotsAllProviderOptions()
    {
        CosmosSnapshotStorageBuilder builder = new()
        {
            ContainerId = "events",
            CosmosClientServiceKey = "cosmos",
            DatabaseId = "database",
            QueryBatchSize = -1,
        };
        SnapshotStorageOptions snapshot = builder.Snapshot();
        builder.DatabaseId = "changed";
        builder.QueryBatchSize = 50;
        Assert.Equal("events", snapshot.ContainerId);
        Assert.Equal("cosmos", snapshot.CosmosClientServiceKey);
        Assert.Equal("database", snapshot.DatabaseId);
        Assert.Equal(-1, snapshot.QueryBatchSize);
        Assert.Empty(builder.Validate());
    }

    /// <summary>
    ///     Verifies a closed nested scope reports its stable diagnostic and rejects mutation.
    /// </summary>
    [Fact]
    public void ClosedScopeRejectsConfiguration()
    {
        CosmosSnapshotStorageBuilder builder = new();
        builder.Close();
        Assert.Equal(
            SnapshotStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
            Assert.Single(builder.Validate()).Code);
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            builder.DatabaseId = "late");
        Assert.Equal(
            SnapshotStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
            Assert.Single(exception.Diagnostics).Code);
    }

    /// <summary>
    ///     Verifies the nested builder starts from the Snapshot Cosmos defaults.
    /// </summary>
    [Fact]
    public void DefaultsShouldBeSensible()
    {
        SnapshotStorageOptions options = new();
        Assert.Equal(SnapshotCosmosDefaults.DatabaseId, options.DatabaseId);
        Assert.Equal(SnapshotCosmosDefaults.ContainerId, options.ContainerId);
        Assert.Equal(SnapshotCosmosDefaults.CosmosClientServiceKey, options.CosmosClientServiceKey);
        Assert.Equal(100, options.QueryBatchSize);
        CosmosSnapshotStorageBuilder builder = new();
        Assert.Equal(options.ContainerId, builder.ContainerId);
        Assert.Equal(options.CosmosClientServiceKey, builder.CosmosClientServiceKey);
        Assert.Equal(options.DatabaseId, builder.DatabaseId);
        Assert.Equal(options.QueryBatchSize, builder.QueryBatchSize);
    }

    /// <summary>
    ///     Verifies each material scalar option failure has its own stable diagnostic.
    /// </summary>
    [Fact]
    public void InvalidOptionsProduceDistinctDiagnostics()
    {
        CosmosSnapshotStorageBuilder builder = new()
        {
            ContainerId = " ",
            CosmosClientServiceKey = string.Empty,
            DatabaseId = " ",
            QueryBatchSize = 0,
        };
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        Assert.Equal(4, diagnostics.Count);
        Assert.Equal(4, diagnostics.Select(diagnostic => diagnostic.Code).Distinct().Count());
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == SnapshotStorageBuilderDiagnosticCodes.ContainerIdRequired);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == SnapshotStorageBuilderDiagnosticCodes.CosmosClientServiceKeyRequired);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == SnapshotStorageBuilderDiagnosticCodes.DatabaseIdRequired);
        Assert.Contains(
            diagnostics,
            diagnostic => diagnostic.Code == SnapshotStorageBuilderDiagnosticCodes.QueryBatchSizeInvalid);
    }

    /// <summary>
    ///     Verifies direct options properties remain settable.
    /// </summary>
    [Fact]
    public void OptionsPropertiesShouldBeSettable()
    {
        SnapshotStorageOptions options = new()
        {
            ContainerId = "custom-container",
            CosmosClientServiceKey = "custom-cosmos",
            DatabaseId = "custom-database",
            QueryBatchSize = 25,
        };
        Assert.Equal("custom-container", options.ContainerId);
        Assert.Equal("custom-cosmos", options.CosmosClientServiceKey);
        Assert.Equal("custom-database", options.DatabaseId);
        Assert.Equal(25, options.QueryBatchSize);
    }

    /// <summary>
    ///     Verifies positive values and the Cosmos dynamic page-size sentinel are supported.
    /// </summary>
    /// <param name="queryBatchSize">The configured query page size.</param>
    /// <param name="valid">Whether the value is supported.</param>
    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-2, false)]
    public void QueryBatchSizeSupportsPositiveOrDynamicValues(
        int queryBatchSize,
        bool valid
    )
    {
        CosmosSnapshotStorageBuilder builder = new()
        {
            QueryBatchSize = queryBatchSize,
        };
        bool hasDiagnostic = builder.Validate()
            .Any(diagnostic => diagnostic.Code == SnapshotStorageBuilderDiagnosticCodes.QueryBatchSizeInvalid);
        Assert.Equal(!valid, hasDiagnostic);
    }

    /// <summary>
    ///     Verifies public Snapshot Cosmos defaults retain their persisted contract values.
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