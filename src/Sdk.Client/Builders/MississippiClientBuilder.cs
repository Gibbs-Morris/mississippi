using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Sdk.Client.Builders;

/// <summary>
///     Builder for Mississippi client registration.
/// </summary>
public sealed class MississippiClientBuilder : IMississippiClientBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiClientBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public MississippiClientBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IMississippiClientBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }

    /// <inheritdoc />
    public IMississippiClientBuilder ConfigureOptions<TOptions>(
        Action<TOptions> configure
    )
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
        return this;
    }
}
