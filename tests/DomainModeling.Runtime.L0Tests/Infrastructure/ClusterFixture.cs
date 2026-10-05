using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.DomainModeling.Runtime.L0Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for UxProjections grain tests.
/// </summary>
internal sealed class ClusterFixture : ClusterFixtureBase<TestSiloConfigurations, DefaultClientConfigurator>;