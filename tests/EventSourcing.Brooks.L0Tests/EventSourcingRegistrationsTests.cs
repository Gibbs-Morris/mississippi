using System;
using System.Linq;


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Factory;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.EventSourcing.Brooks.Reader;


namespace Mississippi.EventSourcing.Brooks.L0Tests;

/// <summary>
///     Tests for <see cref="EventSourcingRegistrations" /> extension methods.
/// </summary>
public sealed class EventSourcingRegistrationsTests
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
        Assert.Equal(MississippiDefaults.StreamProviderName, options.OrleansStreamProviderName);
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
    ///     Verifies that the HostApplicationBuilder extension accepts custom stream provider configuration.
    /// </summary>
    /// <remarks>
    ///     This test verifies options registration without calling <c>Build()</c> on the host builder,
    ///     because building the full host would require complete Orleans membership table configuration.
    ///     The test validates the options are properly registered in the service collection.
    /// </remarks>
    [Fact]
    public void HostApplicationBuilderAddEventSourcingAcceptsCustomStreamProviderName()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.AddEventSourcing(options => options.OrleansStreamProviderName = "CustomStreams");

        // Build a minimal service provider for just the options without triggering full Orleans validation
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        BrookProviderOptions options = provider.GetRequiredService<IOptions<BrookProviderOptions>>().Value;
        Assert.Equal("CustomStreams", options.OrleansStreamProviderName);
    }

    /// <summary>
    ///     Verifies that the HostApplicationBuilder extension registers services (sanity smoke test).
    /// </summary>
    [Fact]
    public void HostApplicationBuilderAddEventSourcingAddsServicesAndConfiguresOrleans()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        // The extension method should complete and register services
        // Note: Host is responsible for configuring streams before calling this
        builder.AddEventSourcing();
        IServiceCollection services = builder.Services;
        Assert.Contains(services, d => d.ServiceType == typeof(IBrookGrainFactory));
        Assert.Contains(services, d => d.ServiceType == typeof(IStreamIdFactory));
    }
}