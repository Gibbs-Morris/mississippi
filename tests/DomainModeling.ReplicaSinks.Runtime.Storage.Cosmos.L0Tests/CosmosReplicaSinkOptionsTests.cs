using System;

using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests option defaults and validation for the Cosmos-backed replica sink provider.
/// </summary>
public sealed class CosmosReplicaSinkOptionsTests
{
    /// <summary>
    ///     Ensures the public defaults keep provisioning conservative and repo-consistent.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkOptionsShouldUseValidateOnlyAndModuleDefaults()
    {
        CosmosReplicaSinkOptions options = new();
        Assert.Equal(ReplicaSinkCosmosDefaults.DatabaseId, options.DatabaseId);
        Assert.Equal(ReplicaSinkCosmosDefaults.ContainerId, options.ContainerId);
        Assert.Equal(ReplicaSinkCosmosDefaults.DefaultQueryBatchSize, options.QueryBatchSize);
        Assert.Equal(ReplicaProvisioningMode.ValidateOnly, options.ProvisioningMode);
    }

    /// <summary>
    ///     Ensures unnamed option validation still uses the default-name path and succeeds when fully configured.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkOptionsValidationShouldAcceptValidUnnamedConfiguration()
    {
        CosmosReplicaSinkOptionsValidation validator = new();
        CosmosReplicaSinkOptions options = new()
        {
            ClientKey = "orders-client",
            DatabaseId = "orders-db",
            ContainerId = "orders-container",
            QueryBatchSize = 25,
            ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing,
        };
        ValidateOptionsResult result = validator.Validate(null, options);
        Assert.True(result.Succeeded);
        Assert.Null(result.Failures);
    }

    /// <summary>
    ///     Ensures option validation rejects each required field and positive batch size independently.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkOptionsValidationShouldRejectEachMissingFieldIndependently()
    {
        CosmosReplicaSinkOptionsValidation validator = new();
        CosmosReplicaSinkOptions missingDatabase = new()
        {
            ClientKey = "orders-client",
            DatabaseId = " ",
            ContainerId = "orders-container",
            QueryBatchSize = 25,
        };
        CosmosReplicaSinkOptions missingContainer = new()
        {
            ClientKey = "orders-client",
            DatabaseId = "orders-db",
            ContainerId = string.Empty,
            QueryBatchSize = 25,
        };
        CosmosReplicaSinkOptions invalidBatchSize = new()
        {
            ClientKey = "orders-client",
            DatabaseId = "orders-db",
            ContainerId = "orders-container",
            QueryBatchSize = 0,
        };
        ValidateOptionsResult missingDatabaseResult = validator.Validate("orders", missingDatabase);
        ValidateOptionsResult missingContainerResult = validator.Validate("orders", missingContainer);
        ValidateOptionsResult invalidBatchSizeResult = validator.Validate("orders", invalidBatchSize);
        Assert.False(missingDatabaseResult.Succeeded);
        Assert.False(missingContainerResult.Succeeded);
        Assert.False(invalidBatchSizeResult.Succeeded);
        Assert.Contains(
            missingDatabaseResult.Failures!,
            failure => failure.Contains("database identifier", StringComparison.Ordinal));
        Assert.Contains(
            missingContainerResult.Failures!,
            failure => failure.Contains("container identifier", StringComparison.Ordinal));
        Assert.Contains(
            invalidBatchSizeResult.Failures!,
            failure => failure.Contains("query batch size", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Ensures option validation rejects empty keys and invalid query batch sizes.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkOptionsValidationShouldRejectInvalidConfiguration()
    {
        CosmosReplicaSinkOptionsValidation validator = new();
        CosmosReplicaSinkOptions options = new()
        {
            ClientKey = string.Empty,
            DatabaseId = " ",
            ContainerId = string.Empty,
            QueryBatchSize = 0,
        };
        ValidateOptionsResult result = validator.Validate("orders", options);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures, failure => failure.Contains("client key", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Ensures option validation rejects missing named options instances.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkOptionsValidationShouldRejectMissingOptionsInstance()
    {
        CosmosReplicaSinkOptionsValidation validator = new();
        ValidateOptionsResult result = validator.Validate("orders", null!);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(
            result.Failures,
            static failure => failure.Contains("options are required", StringComparison.Ordinal));
    }
}