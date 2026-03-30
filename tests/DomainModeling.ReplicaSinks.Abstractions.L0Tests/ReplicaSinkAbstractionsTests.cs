using System;
using System.Reflection;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;


namespace MississippiTests.DomainModeling.ReplicaSinks.Abstractions.L0Tests;

/// <summary>
///     Tests the public replica sink abstraction contracts.
/// </summary>
public sealed class ReplicaSinkAbstractionsTests
{
    /// <summary>
    ///     Ensures empty sink keys are rejected.
    /// </summary>
    [Fact]
    public void ProjectionReplicationAttributeShouldRejectWhitespaceSink()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectionReplicationAttribute(" ", "orders-read", typeof(SampleReplicaContract)));
    }

    /// <summary>
    ///     Ensures mapped replica contracts remain the default path.
    /// </summary>
    [Fact]
    public void ProjectionReplicationAttributeWithContractTypeShouldUseMappedContractPath()
    {
        ProjectionReplicationAttribute attribute = new("search", "orders-read", typeof(SampleReplicaContract));
        Assert.Equal("search", attribute.Sink);
        Assert.Equal("orders-read", attribute.TargetName);
        Assert.Equal(typeof(SampleReplicaContract), attribute.ContractType);
        Assert.False(attribute.IsDirectProjectionReplicationEnabled);
        Assert.Equal(ReplicaWriteMode.LatestState, attribute.WriteMode);
    }

    /// <summary>
    ///     Ensures direct projection replication stays explicit.
    /// </summary>
    [Fact]
    public void ProjectionReplicationAttributeWithoutContractTypeShouldRequireExplicitDirectOptIn()
    {
        ProjectionReplicationAttribute attribute = new("search", "orders-read")
        {
            IsDirectProjectionReplicationEnabled = true,
        };
        Assert.Null(attribute.ContractType);
        Assert.True(attribute.IsDirectProjectionReplicationEnabled);
    }

    /// <summary>
    ///     Ensures direct replication metadata can expose a stable projection-level contract identity.
    /// </summary>
    [Fact]
    public void DirectProjectionMetadataShouldExposeStableReplicaContractIdentity()
    {
        ProjectionReplicationAttribute projectionReplication =
            typeof(SampleDirectReplicaProjection).GetCustomAttribute<ProjectionReplicationAttribute>() ??
            throw new InvalidOperationException("Expected ProjectionReplicationAttribute was not found.");
        ReplicaContractNameAttribute replicaContractName =
            typeof(SampleDirectReplicaProjection).GetCustomAttribute<ReplicaContractNameAttribute>() ??
            throw new InvalidOperationException("Expected ReplicaContractNameAttribute was not found.");
        Assert.Equal("search", projectionReplication.Sink);
        Assert.Equal("orders-direct", projectionReplication.TargetName);
        Assert.Null(projectionReplication.ContractType);
        Assert.True(projectionReplication.IsDirectProjectionReplicationEnabled);
        Assert.Equal("TestApp.Orders.DirectReplica.V1", replicaContractName.ContractIdentity);
    }

    /// <summary>
    ///     Ensures the stable contract identity is composed from the declared segments.
    /// </summary>
    [Fact]
    public void ReplicaContractNameAttributeShouldComposeIdentity()
    {
        ReplicaContractNameAttribute attribute = new("Orders", "Sales", "ReadModel", 2);
        Assert.Equal("Orders.Sales.ReadModel.V2", attribute.ContractIdentity);
    }

    /// <summary>
    ///     Ensures invalid contract versions are rejected.
    /// </summary>
    [Fact]
    public void ReplicaContractNameAttributeShouldRejectVersionLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ReplicaContractNameAttribute("Orders", "Sales", "ReadModel", 0));
    }
}