using Xunit;

namespace Mississippi.AspNetCore.Orleans.L1Tests.Infrastructure;

/// <summary>
/// Defines a test collection that shares a single Orleans TestCluster across all tests.
/// This improves test performance by avoiding repeated cluster initialization.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ClusterTestSuite : ICollectionFixture<ClusterFixture>
{
    public const string Name = "Orleans Cluster Test Suite";
}
