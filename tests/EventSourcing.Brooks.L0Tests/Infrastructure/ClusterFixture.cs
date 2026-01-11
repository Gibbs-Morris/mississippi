using Mississippi.Testing.Utilities.Orleans;


namespace Mississippi.EventSourcing.Brooks.L0Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for EventSourcing grain tests.
/// </summary>
internal sealed class ClusterFixture : ClusterFixtureBase<TestSiloConfigurations, DefaultClientConfigurator>;