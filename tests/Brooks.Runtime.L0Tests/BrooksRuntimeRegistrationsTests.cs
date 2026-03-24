using System;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime.Reader;

using Orleans.Hosting;


namespace Mississippi.Brooks.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="BrooksRuntimeRegistrations" /> extension methods.
/// </summary>
public sealed class BrooksRuntimeRegistrationsTests
{
    /// <summary>
    ///     Verifies that <c>AddEventSourcingByService</c> uses default stream provider name.
    /// </summary>
    [Fact]
    public void AddEventSourcingByServiceUsesDefaultStreamProviderName()
    {
        ServiceCollection services = new();
        services.AddEventSourcingByService();
        using ServiceProvider provider = services.BuildServiceProvider();
        BrookProviderOptions options = provider.GetRequiredService<IOptions<BrookProviderOptions>>().Value;
        Assert.Equal(BrookStreamingDefaults.OrleansStreamProviderName, options.OrleansStreamProviderName);
    }

    /// <summary>
    ///     Verifies that calling <c>AddEventSourcingByService</c> registers the expected singletons and options.
    /// </summary>
    [Fact]
    public void AddEventSourcingRegistersExpectedServicesAndOptions()
    {
        ServiceCollection services = new();
        services.AddEventSourcingByService();

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
    ///     Verifies that the Orleans silo-builder extension accepts custom stream provider configuration.
    /// </summary>
    /// <remarks>
    ///     This test verifies options registration without building a full silo host.
    ///     The test validates the options are properly registered in the silo service collection.
    /// </remarks>
    [Fact]
    public void SiloBuilderAddEventSourcingAcceptsCustomStreamProviderName()
    {
        TestSiloBuilder builder = new();
        builder.AddEventSourcing(options => options.OrleansStreamProviderName = "CustomStreams");

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        BrookProviderOptions options = provider.GetRequiredService<IOptions<BrookProviderOptions>>().Value;
        Assert.Equal("CustomStreams", options.OrleansStreamProviderName);
    }

    /// <summary>
    ///     Verifies that the service-collection onboarding path remains the supported Brooks runtime registration surface.
    /// </summary>
    [Fact]
    public void AddEventSourcingByServiceAddsServicesWithoutHostBuilderOnboarding()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Services.AddEventSourcingByService();

        IServiceCollection services = builder.Services;
        Assert.Contains(services, d => d.ServiceType == typeof(IBrookGrainFactory));
        Assert.Contains(services, d => d.ServiceType == typeof(IStreamIdFactory));
    }
}