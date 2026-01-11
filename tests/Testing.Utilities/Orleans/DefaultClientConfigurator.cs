using System;

using Microsoft.Extensions.Configuration;

using Mississippi.Common.Abstractions;

using Orleans.Hosting;
using Orleans.TestingHost;


namespace Mississippi.Testing.Utilities.Orleans;

/// <summary>
///     Default client configurator that adds memory streams for the default Mississippi stream provider.
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
        clientBuilder.AddMemoryStreams(MississippiDefaults.StreamProviderName);
    }
}