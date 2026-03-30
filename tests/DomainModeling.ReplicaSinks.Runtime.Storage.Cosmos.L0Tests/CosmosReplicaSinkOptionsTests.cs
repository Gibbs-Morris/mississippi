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
}