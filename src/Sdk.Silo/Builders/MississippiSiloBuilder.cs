using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;

using Orleans.Hosting;


namespace Mississippi.Sdk.Silo.Builders;

/// <summary>
///     Builder for Mississippi silo registration.
/// </summary>
public sealed class MississippiSiloBuilder : IMississippiSiloBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiSiloBuilder" /> class.
    /// </summary>
    /// <param name="siloBuilder">The Orleans silo builder.</param>
    public MississippiSiloBuilder(
        ISiloBuilder siloBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        SiloBuilder = siloBuilder;
    }

    private ISiloBuilder SiloBuilder { get; }

    /// <inheritdoc />
    public IServiceCollection Services => SiloBuilder.Services;

    /// <inheritdoc />
    public IMississippiSiloBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }

    /// <inheritdoc />
    public IMississippiSiloBuilder ConfigureOptions<TOptions>(
        Action<TOptions> configure
    )
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
        return this;
    }
}
