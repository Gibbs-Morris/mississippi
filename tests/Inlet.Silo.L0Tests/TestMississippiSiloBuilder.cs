using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Inlet.Silo.L0Tests;

/// <summary>
///     Test Mississippi silo builder for Inlet.Silo L0 tests.
/// </summary>
internal sealed class TestMississippiSiloBuilder : IMississippiSiloBuilder
{
    private readonly ServiceCollection services;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestMississippiSiloBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public TestMississippiSiloBuilder(
        ServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        this.services = services;
    }

    /// <inheritdoc />
    public IMississippiSiloBuilder ConfigureOptions<TOptions>(
        Action<TOptions> configure
    )
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return this;
    }

    /// <inheritdoc />
    public IMississippiSiloBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(services);
        return this;
    }
}