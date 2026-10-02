using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Runtime.Grains;
using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime;
using Mississippi.Hosting.Runtime.Abstractions;
using Mississippi.Testing.Utilities.Mocks;

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

    /// <summary>Captured runtime timing properties cannot mutate the applied configuration after the callback closes.</summary>
    [Fact]
    public void ClosedScopeShouldRejectHeartbeatTimingChanges()
    {
        ServiceCollection services = [];
        AqueductBuilder? captured = null;
        CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct(aqueduct => captured = aqueduct));
        Assert.NotNull(captured);
        Assert.Throws<BuilderValidationException>(() => captured.HeartbeatIntervalMinutes = 5);
        Assert.Throws<BuilderValidationException>(() => captured.DeadServerTimeoutMultiplier = 4);
    }

    /// <summary>The same configuration section preserves gateway heartbeat timing on a separate silo.</summary>
    /// <param name="interval">The configured heartbeat interval in minutes.</param>
    /// <param name="multiplier">The configured dead-server timeout multiplier.</param>
    /// <param name="expectedInterval">The expected effective runtime interval.</param>
    /// <param name="expectedMultiplier">The expected effective runtime multiplier.</param>
    [Theory]
    [InlineData("5", "3", 5, 3)]
    [InlineData("1", "7", 1, 7)]
    public void ConfigurationSectionShouldPreserveHeartbeatTiming(
        string interval,
        string multiplier,
        int expectedInterval,
        int expectedMultiplier
    )
    {
        using ConfigurationRoot configuration = Assert.IsType<ConfigurationRoot>(
            new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [nameof(AqueductOptions.HeartbeatIntervalMinutes)] = interval,
                        [nameof(AqueductOptions.DeadServerTimeoutMultiplier)] = multiplier,
                    })
                .Build());
        ServiceCollection services = [];
        CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct(configuration));
        using ServiceProvider provider = services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal(expectedInterval, options.HeartbeatIntervalMinutes);
        Assert.Equal(expectedMultiplier, options.DeadServerTimeoutMultiplier);
    }

    /// <summary>Cleanup timing rejects nonpositive settings and a timeout outside TimeSpan's range.</summary>
    /// <param name="interval">The configured heartbeat interval.</param>
    /// <param name="multiplier">The configured timeout multiplier.</param>
    [Theory]
    [InlineData("0", "3")]
    [InlineData("1", "0")]
    [InlineData("2147483647", "2147483647")]
    public void ConfigurationSectionShouldRejectUnusableHeartbeatTiming(
        string interval,
        string multiplier
    )
    {
        using ConfigurationRoot configuration = Assert.IsType<ConfigurationRoot>(
            new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [nameof(AqueductOptions.HeartbeatIntervalMinutes)] = interval,
                        [nameof(AqueductOptions.DeadServerTimeoutMultiplier)] = multiplier,
                    })
                .Build());
        ServiceCollection services = [];
        Assert.Throws<BuilderValidationException>(() =>
            CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct(configuration)));
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
    }

    /// <summary>Explicit values configure the same canonical options.</summary>
    [Fact]
    public void ExplicitSettingsAreApplied()
    {
        ServiceCollection services = [];
        CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct("explicit", "server"));
        using ServiceProvider provider = services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal("explicit", options.StreamProviderName);
        Assert.Equal("server", options.ServerStreamNamespace);
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

    /// <summary>Malformed integer settings report the corresponding stable builder diagnostic.</summary>
    /// <param name="propertyName">The configured timing property.</param>
    /// <param name="expectedCode">The diagnostic identifying that property.</param>
    [Theory]
    [InlineData(
        nameof(AqueductOptions.HeartbeatIntervalMinutes),
        AqueductBuilderDiagnosticCodes.HeartbeatIntervalInvalid)]
    [InlineData(
        nameof(AqueductOptions.DeadServerTimeoutMultiplier),
        AqueductBuilderDiagnosticCodes.DeadServerTimeoutInvalid)]
    public void MalformedTimingShouldReportStableDiagnostic(
        string propertyName,
        string expectedCode
    )
    {
        using ConfigurationRoot configuration = Assert.IsType<ConfigurationRoot>(
            new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [propertyName] = "bad",
                    })
                .Build());
        ServiceCollection services = [];
        BuilderValidationException failure = Assert.Throws<BuilderValidationException>(() =>
            CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct(configuration)));
        Assert.Equal(expectedCode, Assert.Single(failure.Diagnostics).Code);
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

    /// <summary>Explicit runtime timing overrides materialize without altering the shared defaults.</summary>
    [Fact]
    public void RuntimeCallbackShouldApplyExplicitHeartbeatTiming()
    {
        ServiceCollection services = [];
        CreateSilo(services)
            .UseMississippi(runtime => runtime.AddAqueduct(aqueduct =>
            {
                aqueduct.HeartbeatIntervalMinutes = 5;
                aqueduct.DeadServerTimeoutMultiplier = 4;
            }));
        using ServiceProvider provider = services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal(5, options.HeartbeatIntervalMinutes);
        Assert.Equal(4, options.DeadServerTimeoutMultiplier);
    }

    /// <summary>Runtime composition preserves gateway-only settings configured by the colocated gateway.</summary>
    [Fact]
    public void RuntimeCompositionPreservesGatewayOnlySettings()
    {
        ServiceCollection services = [];
        services.Configure<AqueductOptions>(options =>
        {
            options.AllClientsStreamNamespace = "gateway-broadcasts";
            options.HeartbeatIntervalMinutes = 11;
            options.DeadServerTimeoutMultiplier = 17;
        });
        CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct());
        using ServiceProvider provider = services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal("gateway-broadcasts", options.AllClientsStreamNamespace);
        Assert.Equal(11, options.HeartbeatIntervalMinutes);
        Assert.Equal(17, options.DeadServerTimeoutMultiplier);
    }

    /// <summary>A gateway configured for five-minute heartbeats stays live until its next heartbeat.</summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task SharedTimingShouldNotExpireHealthyGatewayBetweenHeartbeats()
    {
        using ConfigurationRoot configuration = Assert.IsType<ConfigurationRoot>(
            new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [nameof(AqueductOptions.HeartbeatIntervalMinutes)] = "5",
                        [nameof(AqueductOptions.DeadServerTimeoutMultiplier)] = "3",
                    })
                .Build());
        ServiceCollection services = [];
        CreateSilo(services).UseMississippi(runtime => runtime.AddAqueduct(configuration));
        using ServiceProvider provider = services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        FakeTimeProvider clock = new();
        SignalRServerDirectoryGrain directory = new(
            GrainContextMockBuilder.Create().WithGrainKey("default").BuildObject(),
            Options.Create(options),
            NullLogger<SignalRServerDirectoryGrain>.Instance,
            clock);
        await directory.RegisterServerAsync("five-minute-gateway");
        clock.Advance(TimeSpan.FromMinutes(4));
        TimeSpan runtimeTimeout = TimeSpan.FromMinutes(
            (double)options.HeartbeatIntervalMinutes * options.DeadServerTimeoutMultiplier);
        Assert.True(await directory.IsServerAliveAsync("five-minute-gateway", runtimeTimeout));
    }
}