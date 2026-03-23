using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Hosting.Client.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiClientBuilder" /> and <see cref="WebAssemblyHostBuilderExtensions" />.
/// </summary>
public sealed class MississippiClientBuilderTests
{
    private static WebAssemblyHostBuilder CreateBuilder(
        IServiceCollection services
    )
    {
        WebAssemblyHostBuilder builder =
            (WebAssemblyHostBuilder)RuntimeHelpers.GetUninitializedObject(typeof(WebAssemblyHostBuilder));
        FieldInfo? servicesField = typeof(WebAssemblyHostBuilder).GetField(
            "<Services>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(servicesField);
        servicesField.SetValue(builder, services);
        return builder;
    }

    /// <summary>
    ///     AddMississippiClient configure overload should invoke the callback and return the original host builder.
    /// </summary>
    [Fact]
    public void AddMississippiClientConfigureOverloadReturnsOriginalBuilder()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder builder = CreateBuilder(services);
        MississippiClientBuilder? configuredBuilder = null;
        WebAssemblyHostBuilder result = builder.AddMississippiClient(client => configuredBuilder = client);
        Assert.Same(builder, result);
        Assert.NotNull(configuredBuilder);
        Assert.Same(services, configuredBuilder.Services);
    }

    /// <summary>
    ///     AddMississippiClient should return the client builder.
    /// </summary>
    [Fact]
    public void AddMississippiClientReturnsClientBuilder()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder builder = CreateBuilder(services);
        MississippiClientBuilder clientBuilder = builder.AddMississippiClient();
        Assert.NotNull(clientBuilder);
        Assert.Same(services, clientBuilder.Services);
    }

    /// <summary>
    ///     Reservoir should compose Reservoir services into the host service collection.
    /// </summary>
    [Fact]
    public void ReservoirComposesReservoirServices()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder builder = CreateBuilder(services);
        MississippiClientBuilder clientBuilder = builder.AddMississippiClient();
        clientBuilder.Reservoir(_ => { });
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStore>());
    }

    /// <summary>
    ///     Multiple Reservoir calls should reuse the same underlying Reservoir builder.
    /// </summary>
    [Fact]
    public void ReservoirReusesUnderlyingReservoirBuilderAcrossCalls()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder builder = CreateBuilder(services);
        MississippiClientBuilder clientBuilder = builder.AddMississippiClient();
        IReservoirBuilder? firstBuilder = null;
        IReservoirBuilder? secondBuilder = null;
        clientBuilder.Reservoir(reservoir => firstBuilder = reservoir);
        clientBuilder.Reservoir(reservoir => secondBuilder = reservoir);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.NotNull(firstBuilder);
        Assert.Same(firstBuilder, secondBuilder);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStore>());
    }
}