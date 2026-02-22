using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.Brooks.Runtime.L0Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for EventSourcing grain tests.
/// </summary>
internal sealed class ClusterFixture : ClusterFixtureBase<TestSiloConfigurations, DefaultClientConfigurator>;