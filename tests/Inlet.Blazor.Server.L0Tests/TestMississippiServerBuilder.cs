using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Inlet.Blazor.Server.L0Tests;

/// <summary>
///     Test Mississippi server builder for Inlet.Blazor.Server L0 tests.
/// </summary>
internal sealed class TestMississippiServerBuilder : IMississippiServerBuilder
{
    private IServiceCollection Services { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestMississippiServerBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public TestMississippiServerBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
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

    /// <inheritdoc />
    public IMississippiServerBuilder ConfigureServices(
        Action<IServiceCollection> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }
}