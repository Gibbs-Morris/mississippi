using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Inlet.Server.L0Tests;

/// <summary>
///     Test Mississippi server builder for Inlet.Server L0 tests.
/// </summary>
internal sealed class TestMississippiServerBuilder : IMississippiServerBuilder
{
    private readonly ServiceCollection services;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestMississippiServerBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public TestMississippiServerBuilder(
        ServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        this.services = services;
    }

    /// <inheritdoc />
    public IMississippiServerBuilder ConfigureOptions<TOptions>(
        Action<TOptions> configure
    )
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return this;
    }

    /// <inheritdoc />
    public IMississippiServerBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(services);
        return this;
    }
}