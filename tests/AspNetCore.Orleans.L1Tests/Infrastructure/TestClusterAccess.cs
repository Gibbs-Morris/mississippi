using System;
using Orleans.TestingHost;

namespace Mississippi.AspNetCore.Orleans.L1Tests.Infrastructure;

/// <summary>
/// Provides static access to the shared TestCluster for tests that need it.
/// This is used by test classes that have ClusterFixture injected.
/// </summary>
public static class TestClusterAccess
{
    private static TestCluster? _cluster;

    public static TestCluster Cluster
    {
        get => _cluster ?? throw new InvalidOperationException("TestCluster not initialized. Ensure ClusterFixture is properly injected.");
        set => _cluster = value;
    }
}
