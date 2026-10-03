using Mississippi.Testing.Utilities.Orleans;

using MississippiTests.Aqueduct.Gateway.L1Tests.Infrastructure;


[assembly: AssemblyFixture(typeof(ClusterFixture))]

namespace MississippiTests.Aqueduct.Gateway.L1Tests.Infrastructure;

/// <summary>
///     Shared Orleans TestCluster fixture for SignalR Orleans grain tests.
/// </summary>
internal sealed class ClusterFixture : ClusterFixtureBase<TestSiloConfigurations, DefaultClientConfigurator>;