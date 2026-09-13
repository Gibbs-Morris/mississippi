using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime;
using Mississippi.Hosting.Runtime.Abstractions;

using NSubstitute;

using Orleans.Hosting;
using Orleans.Storage;
using Orleans.Streams;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>Verifies Aqueduct composition through the real runtime attachment boundary.</summary>
public sealed class AqueductRuntimeRegistrationsTests
{
    private static ISiloBuilder CreateSilo(
        IServiceCollection services
    )
    {
        ISiloBuilder silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(services);
        silo.Configuration.Returns(Substitute.For<IConfiguration>());
        return silo;
    }

    /// <summary>Late composition is rejected by the native lifecycle before its callback runs.</summary>
    [Fact]
    public void AppliedAndClosedRootsRejectComposition()
    {
        ServiceCollection services = [];
        ISiloBuilder silo = CreateSilo(services);
        IRuntimeBuilder? captured = null;
        bool invoked = false;
        silo.UseMississippi(runtime =>
        {
            captured = runtime;
            runtime.ApplyToSilo(silo);
            Assert.Throws<BuilderValidationException>(() => runtime.AddAqueduct(_ => invoked = true));
        });
        Assert.NotNull(captured);
        Assert.Throws<BuilderValidationException>(() => captured.AddAqueduct(_ => invoked = true));
        Assert.False(invoked);
    }

    /// <summary>Configuration sections use defaults for omitted stream settings.</summary>
    [Fact]
    public void ConfigurationSectionUsesDefaultsForOmittedSettings()
    {
        using ConfigurationRoot configuration = Assert.IsType<ConfigurationRoot>(
            new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [nameof(AqueductOptions.StreamProviderName)] = "configured",
                    })
                .Build());
        ServiceCollection services = [];
        ISiloBuilder silo = CreateSilo(services);
        silo.UseMississippi(runtime => runtime.AddAqueduct(configuration));
        using ServiceProvider provider = services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal("configured", options.StreamProviderName);
        Assert.Equal(AqueductStreamDefaults.ServerStreamNamespace, options.ServerStreamNamespace);
        Assert.Equal(AqueductStreamDefaults.AllClientsStreamNamespace, options.AllClientsStreamNamespace);
    }

    /// <summary>One runtime call registers the built-in factory when no custom factory exists.</summary>
    [Fact]
    public void DefaultCompositionRegistersBuiltInFactory()
    {
        ServiceCollection services = [];
        CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct());
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(IAqueductGrainFactory)) &&
                          (descriptor.ImplementationType == typeof(AqueductGrainFactory)));
    }

    /// <summary>One runtime call registers the default backplane options and factory.</summary>
    [Fact]
    public void DefaultCompositionRegistersOptionsAndPreservesCustomFactories()
    {
        ServiceCollection services = [];
        IAqueductGrainFactory custom = Substitute.For<IAqueductGrainFactory>();
        services.AddSingleton(custom);
        CreateSilo(services).UseMississippi(runtime => Assert.Same(runtime, runtime.AddAqueduct()));
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(custom, provider.GetRequiredService<IAqueductGrainFactory>());
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal(AqueductStreamDefaults.StreamProviderName, options.StreamProviderName);
    }

    /// <summary>Duplicate configuration fails before the second callback can run.</summary>
    [Fact]
    public void DuplicateCompositionIsRejected()
    {
        ServiceCollection services = [];
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() => CreateSilo(services)
            .UseMississippi(runtime =>
            {
                runtime.AddAqueduct();
                runtime.AddAqueduct(_ => invoked = true);
            }));
        Assert.Equal(AqueductBuilderDiagnosticCodes.DuplicateComposition, Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
        Assert.Empty(services);
    }

    /// <summary>Settings are applied once and captured nested scopes close before terminal completion.</summary>
    [Fact]
    public void ExplicitApplicationSnapshotsSettingsAndClosesTheNestedScope()
    {
        ServiceCollection services = [];
        ISiloBuilder silo = CreateSilo(services);
        AqueductBuilder? captured = null;
        int calls = 0;
        silo.UseMississippi(runtime =>
        {
            runtime.AddAqueduct(aqueduct =>
            {
                calls++;
                captured = aqueduct;
                aqueduct.StreamProviderName = "streams";
                aqueduct.ServerStreamNamespace = "servers";
                aqueduct.AllClientsStreamNamespace = "broadcasts";
            });
            Assert.Equal(0, calls);
            runtime.ApplyToSilo(silo);
            Assert.NotNull(captured);
            Assert.Throws<BuilderValidationException>(() => captured.StreamProviderName = "late");
        });
        Assert.Equal(1, calls);
        using ServiceProvider provider = services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal("streams", options.StreamProviderName);
        Assert.Equal("servers", options.ServerStreamNamespace);
        Assert.Equal("broadcasts", options.AllClientsStreamNamespace);
    }

    /// <summary>Explicit values configure the same canonical options.</summary>
    [Fact]
    public void ExplicitSettingsAreApplied()
    {
        ServiceCollection services = [];
        CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct("explicit", "server", "all"));
        using ServiceProvider provider = services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal("explicit", options.StreamProviderName);
        Assert.Equal("server", options.ServerStreamNamespace);
        Assert.Equal("all", options.AllClientsStreamNamespace);
    }

    /// <summary>Callback failures close nested scopes and allow a fresh root attempt.</summary>
    [Fact]
    public void FailedCallbackClosesScopeWithoutPublishing()
    {
        ServiceCollection services = [];
        ISiloBuilder silo = CreateSilo(services);
        AqueductBuilder? captured = null;
        InvalidOperationException expected = new("Configuration failed.");
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() => silo.UseMississippi(runtime =>
                runtime.AddAqueduct(aqueduct =>
                {
                    captured = aqueduct;
                    throw expected;
                }))));
        Assert.Empty(services);
        Assert.NotNull(captured);
        Assert.Throws<BuilderValidationException>(() => captured.UseMemoryStreams());
        silo.UseMississippi(runtime => runtime.AddAqueduct());
    }

    /// <summary>Invalid nested settings fail terminal attachment and permit a retry on the same host.</summary>
    [Fact]
    public void InvalidSettingsCanRetryAfterTerminalFailure()
    {
        ServiceCollection services = [];
        ISiloBuilder silo = CreateSilo(services);
        AqueductBuilder? captured = null;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            silo.UseMississippi(runtime => runtime.AddAqueduct(aqueduct =>
            {
                captured = aqueduct;
                aqueduct.StreamProviderName = " ";
            })));
        Assert.Equal(AqueductBuilderDiagnosticCodes.StreamProviderRequired, Assert.Single(exception.Diagnostics).Code);
        Assert.Empty(services);
        Assert.NotNull(captured);
        Assert.Throws<BuilderValidationException>(() => captured.UseMemoryStreams());
        silo.UseMississippi(runtime => runtime.AddAqueduct());
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(
            AqueductStreamDefaults.StreamProviderName,
            provider.GetRequiredService<IOptions<AqueductOptions>>().Value.StreamProviderName);
    }

    /// <summary>Memory infrastructure uses the final provider selection and the required PubSubStore.</summary>
    [Fact]
    public void MemoryStreamsUseTheFinalProviderSelection()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        hostBuilder.UseOrleans(silo => silo.UseLocalhostClustering()
            .UseMississippi(runtime => runtime.AddAqueduct(aqueduct =>
            {
                aqueduct.UseMemoryStreams("initial");
                aqueduct.StreamProviderName = "final";
            })));
        using IHost host = hostBuilder.Build();
        Assert.NotNull(host.Services.GetRequiredKeyedService<IStreamProvider>("final"));
        Assert.NotNull(host.Services.GetRequiredKeyedService<IGrainStorage>("PubSubStore"));
        Assert.Equal("final", host.Services.GetRequiredService<IOptions<AqueductOptions>>().Value.StreamProviderName);
    }

    /// <summary>Invalid public arguments fail at the registration boundary.</summary>
    [Fact]
    public void NullArgumentsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => AqueductRuntimeRegistrations.AddAqueduct(null!));
        Assert.Throws<ArgumentNullException>(() =>
            AqueductRuntimeRegistrations.AddAqueduct(null!, (IConfiguration)null!));
    }

    /// <summary>Later options configuration cannot silently introduce an invalid stream name.</summary>
    [Fact]
    public void OptionsValidationRejectsLaterInvalidStreamName()
    {
        ServiceCollection services = [];
        CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct());
        services.PostConfigure<AqueductOptions>(options => options.StreamProviderName = " ");
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<AqueductOptions>>().Value);
    }

    /// <summary>Runtime composition preserves timing settings configured by the colocated gateway.</summary>
    [Fact]
    public void RuntimeCompositionPreservesGatewayTimingSettings()
    {
        ServiceCollection services = [];
        services.Configure<AqueductOptions>(options =>
        {
            options.HeartbeatIntervalMinutes = 11;
            options.DeadServerTimeoutMultiplier = 17;
        });
        CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct());
        using ServiceProvider provider = services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal(11, options.HeartbeatIntervalMinutes);
        Assert.Equal(17, options.DeadServerTimeoutMultiplier);
    }
}