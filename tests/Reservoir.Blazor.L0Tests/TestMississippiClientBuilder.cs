using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Test Mississippi client builder for Reservoir.Blazor L0 tests.
/// </summary>
internal sealed class TestMississippiClientBuilder : IMississippiClientBuilder
{
    private readonly ServiceCollection services;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestMississippiClientBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public TestMississippiClientBuilder(
        ServiceCollection services
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