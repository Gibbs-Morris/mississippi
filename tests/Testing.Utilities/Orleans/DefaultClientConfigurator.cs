using System;

using Microsoft.Extensions.Configuration;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.Testing.Utilities.Orleans;

/// <summary>
///     Default client configurator that adds memory streams for the MississippiBrookStreamProvider.
/// </summary>
public sealed class DefaultClientConfigurator : IClientBuilderConfigurator
{
    /// <inheritdoc />
    public void Configure(
        IConfiguration configuration,
        IClientBuilder clientBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);
        clientBuilder.AddMemoryStreams("MississippiBrookStreamProvider");
    }
}