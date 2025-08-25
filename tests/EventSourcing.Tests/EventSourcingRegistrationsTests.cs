using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;


namespace Mississippi.EventSourcing.Tests;

/// <summary>
///     Tests for <see cref="EventSourcingRegistrations" /> extension methods.
/// </summary>
public class EventSourcingRegistrationsTests
{
    /// <summary>
    ///     Verifies that calling <c>AddEventSourcing</c> registers the expected singletons and options.
    /// </summary>
    [Fact]
    public void AddEventSourcingRegistersExpectedServicesAndOptions()
    {
        ServiceCollection services = new();
        services.AddEventSourcing();

        // Check that IBrookGrainFactory and IStreamIdFactory are registered as singletons
        ServiceDescriptor? grainFactory = services.FirstOrDefault(d => d.ServiceType == typeof(IBrookGrainFactory));
        ServiceDescriptor? streamIdFactory = services.FirstOrDefault(d => d.ServiceType == typeof(IStreamIdFactory));
        Assert.NotNull(grainFactory);
        Assert.Equal(ServiceLifetime.Singleton, grainFactory!.Lifetime);
        Assert.NotNull(streamIdFactory);
        Assert.Equal(ServiceLifetime.Singleton, streamIdFactory!.Lifetime);

        // Build provider and resolve options to ensure the AddOptions registrations were applied.
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<BrookReaderOptions>? readerOptions = provider.GetService<IOptions<BrookReaderOptions>>();
        IOptions<BrookProviderOptions>? providerOptions = provider.GetService<IOptions<BrookProviderOptions>>();
        Assert.NotNull(readerOptions);
        Assert.NotNull(providerOptions);
    }

    /// <summary>
    ///     Verifies that the HostApplicationBuilder extension registers services (sanity smoke test).
    /// </summary>
    [Fact]
    public void HostApplicationBuilderAddEventSourcingAddsServicesAndConfiguresOrleans()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        // The extension method should complete and register services
        builder.AddEventSourcing();
        IServiceCollection services = builder.Services;
        Assert.Contains(services, d => d.ServiceType == typeof(IBrookGrainFactory));
        Assert.Contains(services, d => d.ServiceType == typeof(IStreamIdFactory));
    }
}