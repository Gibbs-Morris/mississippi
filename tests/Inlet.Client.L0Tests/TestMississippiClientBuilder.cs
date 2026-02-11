using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Test Mississippi client builder for Inlet.Client L0 tests.
/// </summary>
internal sealed class TestMississippiClientBuilder : IMississippiClientBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestMississippiClientBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public TestMississippiClientBuilder(
        ServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    private ServiceCollection Services { get; }

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

    /// <inheritdoc />
    public IMississippiClientBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }
}