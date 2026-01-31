using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Snapshots.Cosmos;


namespace Mississippi.Sdk.Silo.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiSiloRegistrations" /> and provider helpers.
/// </summary>
public sealed class MississippiSiloRegistrationsTests
{
    /// <summary>
    ///     AddCosmosProviders should configure storage and stream provider options.
    /// </summary>
    [Fact]
    public void AddCosmosProvidersConfiguresCosmosStorageOptions()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        MississippiSiloBuilder mississippiBuilder = builder.AddMississippiSilo();
        mississippiBuilder.AddCosmosProviders(options =>
        {
            options.DatabaseId = "test-db";
            options.ContainerPrefix = "app-";
            options.StreamProviderName = "test-streams";
        });
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IOptions<BrookStorageOptions> brookOptions = provider.GetRequiredService<IOptions<BrookStorageOptions>>();
        IOptions<SnapshotStorageOptions> snapshotOptions =
            provider.GetRequiredService<IOptions<SnapshotStorageOptions>>();
        IOptions<BrookProviderOptions> providerOptions = provider.GetRequiredService<IOptions<BrookProviderOptions>>();
        Assert.Equal("test-db", brookOptions.Value.DatabaseId);
        Assert.Equal("app-brooks", brookOptions.Value.ContainerId);
        Assert.Equal("app-locks", brookOptions.Value.LockContainerName);
        Assert.Equal("test-db", snapshotOptions.Value.DatabaseId);
        Assert.Equal("app-snapshots", snapshotOptions.Value.ContainerId);
        Assert.Equal("test-streams", providerOptions.Value.OrleansStreamProviderName);
    }

    /// <summary>
    ///     AddInMemoryProviders should configure the brook stream provider options.
    /// </summary>
    [Fact]
    public void AddInMemoryProvidersConfiguresBrookProviderOptions()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        MississippiSiloBuilder mississippiBuilder = builder.AddMississippiSilo();
        mississippiBuilder.AddInMemoryProviders(options => options.StreamProviderName = "test-streams");
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IOptions<BrookProviderOptions> options = provider.GetRequiredService<IOptions<BrookProviderOptions>>();
        Assert.Equal("test-streams", options.Value.OrleansStreamProviderName);
    }

    /// <summary>
    ///     AddMississippiSilo should register options and apply configuration.
    /// </summary>
    [Fact]
    public void AddMississippiSiloRegistersOptionsAndAppliesConfiguration()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddMississippiSilo(options => options.EnableDashboard = false);
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IOptions<MississippiSiloOptions> options = provider.GetRequiredService<IOptions<MississippiSiloOptions>>();
        Assert.False(options.Value.EnableDashboard);
    }
}