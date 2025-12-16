using System;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Mississippi.AspNetCore.Orleans.L1Tests.Infrastructure;

/// <summary>
/// Provides a shared Orleans TestCluster for L1 integration tests.
/// Ensures a single cluster is created for all tests to improve performance.
/// </summary>
public sealed class ClusterFixture : IDisposable
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public TestCluster Cluster { get; }

    public void Dispose()
    {
        Cluster?.StopAllSilos();
    }

    private sealed class TestSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
            siloBuilder.AddMemoryGrainStorage("CacheStorage");
            siloBuilder.AddMemoryGrainStorage("OutputCacheStorage");
            siloBuilder.AddMemoryGrainStorage("TicketStorage");
            siloBuilder.AddMemoryGrainStorage("SignalRStorage");
            siloBuilder.AddMemoryStreams("StreamProvider");
            siloBuilder.AddMemoryStreams("SignalRStreamProvider");
        }
    }
}
