using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime.L0Tests;

/// <summary>
///     Provides an in-memory silo composition surface without starting Orleans.
/// </summary>
internal sealed class TestSiloBuilder : ISiloBuilder
{
    /// <inheritdoc />
    public IConfiguration Configuration { get; } = Mock.Of<IConfiguration>();

    /// <inheritdoc />
    public IServiceCollection Services { get; } = new ServiceCollection();
}