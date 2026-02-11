using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions;
using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Brooks.Abstractions.Factory;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.EventSourcing.Brooks.Reader;


namespace Mississippi.EventSourcing.Brooks.L0Tests;

/// <summary>
///     Tests for <see cref="EventSourcingRegistrations" /> extension methods.
/// </summary>
public sealed class EventSourcingRegistrationsTests
{
    private sealed class TestMississippiSiloBuilder : IMississippiSiloBuilder
    {
        private IServiceCollection Services { get; }

        public TestMississippiSiloBuilder(
            IServiceCollection services
        )
        {
            ArgumentNullException.ThrowIfNull(services);
            Services = services;
        }

        public IMississippiSiloBuilder ConfigureOptions<TOptions>(
            Action<TOptions> configure
        )
            where TOptions : class
        {
            Services.Configure(configure);
            return this;
        }

        public IMississippiSiloBuilder ConfigureServices(
            Action<IServiceCollection> configure
        )
        {
            configure(Services);
            return this;
        }
    }

    /// <summary>
    ///     Verifies that <c>AddEventSourcingByService</c> uses default stream provider name.
    /// </summary>
    [Fact]
    public void AddEventSourcingByServiceUsesDefaultStreamProviderName()
    {
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);
        builder.AddEventSourcing();
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
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);
        builder.AddEventSourcing();

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
}