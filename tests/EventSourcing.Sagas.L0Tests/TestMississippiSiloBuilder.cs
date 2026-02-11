using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Test Mississippi silo builder for EventSourcing.Sagas L0 tests.
/// </summary>
internal sealed class TestMississippiSiloBuilder : IMississippiSiloBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestMississippiSiloBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public TestMississippiSiloBuilder(
        ServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    private ServiceCollection Services { get; }

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

    /// <inheritdoc />
    public IMississippiSiloBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }
}