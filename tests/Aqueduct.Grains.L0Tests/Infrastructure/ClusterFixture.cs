using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Aqueduct.Grains.L0Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for Aqueduct Grains tests.
/// </summary>
internal sealed class ClusterFixture : ClusterFixtureBase<TestSiloConfigurations, DefaultClientConfigurator>;