using System;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime.Factory;
using Mississippi.Brooks.Runtime.Reader;
using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime;
using Mississippi.Hosting.Runtime.Abstractions;

using Moq;

using Orleans;
using Orleans.Hosting;


namespace Mississippi.Brooks.Runtime.L0Tests;

/// <summary>
///     Verifies the builder-first Brooks registration contract.
/// </summary>
public sealed class BrooksRuntimeRegistrationsTests
{
    private static ISiloBuilder CreateSilo(
        IServiceCollection services
    )
    {
        IConfiguration configuration = Mock.Of<IConfiguration>();
        return Mock.Of<ISiloBuilder>(silo => (silo.Services == services) && (silo.Configuration == configuration));
    }

    /// <summary>
    ///     Stream-provider configuration no longer requires a separate native registration call.
    /// </summary>
    [Fact]
    public void AddEventSourcingConfiguresTheChosenStreamProvider()
    {
        ServiceCollection services = [];
        CreateSilo(services)
            .UseMississippi(runtime => Assert.Same(
                runtime,
                runtime.AddEventSourcing(options => options.OrleansStreamProviderName = "CustomStreams")));
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(
            "CustomStreams",
            provider.GetRequiredService<IOptions<BrookProviderOptions>>().Value.OrleansStreamProviderName);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IBrookGrainFactory));
    }

    /// <summary>
    ///     One builder call registers core factories and default options together.
    /// </summary>
    [Fact]
    public void AddEventSourcingRegistersFactoriesAndDefaultOptions()
    {
        ServiceCollection services = [];
        CreateSilo(services).UseMississippi(runtime => runtime.AddEventSourcing());
        Assert.Equal(
            ServiceLifetime.Singleton,
            Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IBrookGrainFactory)).Lifetime);
        Assert.Equal(
            ServiceLifetime.Singleton,
            Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStreamIdFactory)).Lifetime);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IOptions<BrookReaderOptions>>());
        Assert.Equal(
            BrookStreamingDefaults.OrleansStreamProviderName,
            provider.GetRequiredService<IOptions<BrookProviderOptions>>().Value.OrleansStreamProviderName);
    }

    /// <summary>
    ///     Captured runtime scopes cannot register services after terminal attachment.
    /// </summary>
    [Fact]
    public void CompletedRuntimeRejectsBrooksComposition()
    {
        ServiceCollection services = [];
        IRuntimeBuilder? captured = null;
        CreateSilo(services).UseMississippi(runtime => captured = runtime);
        Assert.NotNull(captured);
        ServiceDescriptor[] original = services.ToArray();
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            captured.AddEventSourcing());
        Assert.Equal(BuilderDiagnosticCodes.BuilderAlreadyAttached, Assert.Single(exception.Diagnostics).Code);
        Assert.Equal(original, services);
    }

    /// <summary>
    ///     An existing singleton factory callback remains authoritative without moving its descriptor.
    /// </summary>
    [Fact]
    public void ExistingConcreteSingletonConfigurationIsPreserved()
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddSingleton(Mock.Of<IGrainFactory>());
        int calls = 0;
        ServiceDescriptor configured = ServiceDescriptor.Singleton<BrookGrainFactory>(provider =>
        {
            calls++;
            return ActivatorUtilities.CreateInstance<BrookGrainFactory>(provider);
        });
        int position = services.Count;
        ((IServiceCollection)services).Add(configured);
        CreateSilo(services).UseMississippi(runtime => runtime.AddEventSourcing());
        Assert.Same(configured, services[position]);
        using ServiceProvider provider = services.BuildServiceProvider();
        BrookGrainFactory canonical = provider.GetRequiredService<BrookGrainFactory>();
        Assert.Same(canonical, provider.GetRequiredService<IBrookGrainFactory>());
        Assert.Same(canonical, provider.GetRequiredService<IInternalBrookGrainFactory>());
        Assert.Equal(1, calls);
    }

    /// <summary>
    ///     Existing host customizations survive default Brooks composition.
    /// </summary>
    [Fact]
    public void ExistingStreamFactoryIsPreserved()
    {
        ServiceCollection services = [];
        IStreamIdFactory custom = Mock.Of<IStreamIdFactory>();
        services.AddSingleton(custom);
        CreateSilo(services).UseMississippi(runtime => runtime.AddEventSourcing());
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(custom, provider.GetRequiredService<IStreamIdFactory>());
    }

    /// <summary>
    ///     Public and internal grain access use the same canonical factory even when the host registered another one.
    /// </summary>
    /// <param name="existingLifetime">The lifetime of the pre-existing concrete factory registration.</param>
    /// <param name="duplicateExisting">Whether the host registered the concrete factory more than once.</param>
    [Theory]
    [InlineData(ServiceLifetime.Singleton, false)]
    [InlineData(ServiceLifetime.Singleton, true)]
    [InlineData(ServiceLifetime.Scoped, false)]
    [InlineData(ServiceLifetime.Scoped, true)]
    [InlineData(ServiceLifetime.Transient, false)]
    [InlineData(ServiceLifetime.Transient, true)]
    public void GrainFactoryRegistrationsRemainAuthoritativeAndConsistent(
        ServiceLifetime existingLifetime,
        bool duplicateExisting
    )
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddSingleton(Mock.Of<IGrainFactory>());
        ((IServiceCollection)services).Add(
            ServiceDescriptor.Describe(typeof(BrookGrainFactory), typeof(BrookGrainFactory), existingLifetime));
        if (duplicateExisting)
        {
            ((IServiceCollection)services).Add(
                ServiceDescriptor.Describe(typeof(BrookGrainFactory), typeof(BrookGrainFactory), existingLifetime));
        }

        IBrookGrainFactory custom = Mock.Of<IBrookGrainFactory>();
        services.AddSingleton(custom);
        CreateSilo(services).UseMississippi(runtime => runtime.AddEventSourcing());
        using ServiceProvider provider = services.BuildServiceProvider();
        BrookGrainFactory canonical = provider.GetRequiredService<BrookGrainFactory>();
        Assert.Same(canonical, provider.GetRequiredService<IBrookGrainFactory>());
        Assert.Same(canonical, provider.GetRequiredService<IInternalBrookGrainFactory>());
        Assert.Same(canonical, provider.GetRequiredService<BrookGrainFactory>());
        using IServiceScope scope = provider.CreateScope();
        Assert.Same(canonical, scope.ServiceProvider.GetRequiredService<BrookGrainFactory>());
        Assert.NotSame(custom, provider.GetRequiredService<IBrookGrainFactory>());
        Assert.Same(canonical, Assert.Single(provider.GetServices<BrookGrainFactory>()));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IBrookGrainFactory));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IInternalBrookGrainFactory));
    }

    /// <summary>
    ///     Null runtime builders are rejected at the public boundary.
    /// </summary>
    [Fact]
    public void NullRuntimeIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => BrooksRuntimeRegistrations.AddEventSourcing(null!));
    }

    /// <summary>
    ///     Repeated composition keeps one factory while preserving intentional option configuration order.
    /// </summary>
    [Fact]
    public void RepeatedCompositionDoesNotDuplicateFactories()
    {
        ServiceCollection services = [];
        CreateSilo(services)
            .UseMississippi(runtime =>
            {
                runtime.AddEventSourcing(options => options.OrleansStreamProviderName = "First");
                runtime.AddEventSourcing(options => options.OrleansStreamProviderName = "Second");
            });
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IBrookGrainFactory));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStreamIdFactory));
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(
            "Second",
            provider.GetRequiredService<IOptions<BrookProviderOptions>>().Value.OrleansStreamProviderName);
    }
}