using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.AspNetCore.SignalR.Orleans.L0Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for SignalR Orleans grain tests.
/// </summary>
internal sealed class ClusterFixture : ClusterFixtureBase<TestSiloConfigurations, DefaultClientConfigurator>;