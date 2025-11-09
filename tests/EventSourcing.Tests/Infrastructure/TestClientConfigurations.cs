using Microsoft.Extensions.Configuration;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Infrastructure;

/// <summary>
///     Client configuration for the Orleans test cluster.
/// </summary>
internal sealed class TestClientConfigurations : IClientBuilderConfigurator
{
    /// <inheritdoc />
    public void Configure(
        IConfiguration configuration,
        IClientBuilder clientBuilder
    )
    {
        clientBuilder.AddMemoryStreams("MississippiBrookStreamProvider");
    }
}