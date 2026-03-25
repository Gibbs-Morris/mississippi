using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Orleans.Hosting;


namespace Mississippi.Brooks.Runtime.L0Tests;

/// <summary>
///     Minimal silo-builder test double for Brooks registration tests that only need service and configuration access.
/// </summary>
internal sealed class TestSiloBuilder : ISiloBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestSiloBuilder" /> class.
    /// </summary>
    internal TestSiloBuilder()
    {
        Services = new ServiceCollection();
        Configuration = new ConfigurationBuilder().Build();
    }

    /// <summary>
    ///     Gets the configuration exposed through the test double.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    ///     Gets the service collection exposed through the test double.
    /// </summary>
    public IServiceCollection Services { get; }
}