using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Client.Abstractions;


namespace Mississippi.Common.Builders.Client;

/// <summary>
///     Concrete client-host builder implementation.
/// </summary>
public sealed class ClientBuilder : IClientBuilder
{
    private ClientBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Creates a new client builder instance.
    /// </summary>
    /// <returns>A new <see cref="ClientBuilder" /> instance.</returns>
    public static ClientBuilder Create() => new(new ServiceCollection());
}