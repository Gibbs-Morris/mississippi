using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Inlet.Silo.L0Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for Inlet Orleans grain tests.
/// </summary>
internal sealed class ClusterFixture : ClusterFixtureBase<TestSiloConfigurations, DefaultClientConfigurator>;
