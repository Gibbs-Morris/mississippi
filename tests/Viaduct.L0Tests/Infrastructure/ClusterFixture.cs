using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Viaduct.L0Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for SignalR Orleans grain tests.
/// </summary>
internal sealed class ClusterFixture : ClusterFixtureBase<TestSiloConfigurations, DefaultClientConfigurator>;