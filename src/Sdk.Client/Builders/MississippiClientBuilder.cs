using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Sdk.Client.Builders;

/// <summary>
///     Builder for Mississippi client registration.
/// </summary>
public sealed class MississippiClientBuilder : IMississippiClientBuilder
{
    private readonly IServiceCollection services;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiClientBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public MississippiClientBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        this.services = services;
    }

    /// <inheritdoc />
    public IMississippiClientBuilder ConfigureOptions<TOptions>(
        Action<TOptions> configure
    )
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return this;
    }

    /// <inheritdoc />
    public IMississippiClientBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(services);
        return this;
    }
}