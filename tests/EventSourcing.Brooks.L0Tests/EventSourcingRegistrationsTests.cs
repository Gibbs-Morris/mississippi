using System;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions.Factory;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.EventSourcing.Brooks.Reader;


namespace Mississippi.EventSourcing.Brooks.L0Tests;

/// <summary>
///     Tests for <see cref="EventSourcingRegistrations" /> extension methods.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks")]
[AllureSubSuite("Event Sourcing Registrations")]
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
        Assert.Equal("MississippiBrookStreamProvider", options.OrleansStreamProviderName);
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
    [Fact]
    public void HostApplicationBuilderAddEventSourcingAcceptsCustomStreamProviderName()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.AddEventSourcing(options => options.OrleansStreamProviderName = "CustomStreams");
        using IHost host = builder.Build();
        BrookProviderOptions options = host.Services.GetRequiredService<IOptions<BrookProviderOptions>>().Value;
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