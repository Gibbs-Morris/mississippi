using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Keeps native Orleans registrations in the runtime's staged service collection.
/// </summary>
internal sealed class StagedSiloBuilder : ISiloBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StagedSiloBuilder" /> class.
    /// </summary>
    /// <param name="services">The staged services.</param>
    /// <param name="configuration">The owning host configuration.</param>
    public StagedSiloBuilder(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        Services = services;
        Configuration = configuration;
    }

    /// <inheritdoc />
    public IConfiguration Configuration { get; }

    /// <inheritdoc />
    public IServiceCollection Services { get; }
}