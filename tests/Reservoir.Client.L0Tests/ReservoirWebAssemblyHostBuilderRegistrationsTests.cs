using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Client.L0Tests;

/// <summary>
///     Tests for <see cref="ReservoirWebAssemblyHostBuilderRegistrations" />.
/// </summary>
public sealed class ReservoirWebAssemblyHostBuilderRegistrationsTests
{
    /// <summary>
    ///     AddReservoir should register Reservoir services on a WebAssembly host builder.
    /// </summary>
    [Fact]
    public void AddReservoirRegistersReservoirServicesOnWebAssemblyHostBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        WebAssemblyHostBuilder builder = CreateBuilder(services);

        // Act
        IReservoirBuilder reservoirBuilder = builder.AddReservoir();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // Assert
        Assert.Same(services, reservoirBuilder.Services);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStore>());
    }

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
}