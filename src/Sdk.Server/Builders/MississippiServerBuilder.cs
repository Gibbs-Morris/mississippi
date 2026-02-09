using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Sdk.Server.Builders;

/// <summary>
///     Builder for Mississippi server registration.
/// </summary>
public sealed class MississippiServerBuilder : IMississippiServerBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiServerBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public MississippiServerBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IMississippiServerBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }

    /// <inheritdoc />
    public IMississippiServerBuilder ConfigureOptions<TOptions>(
        Action<TOptions> configure
    )
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
        return this;
    }
}
