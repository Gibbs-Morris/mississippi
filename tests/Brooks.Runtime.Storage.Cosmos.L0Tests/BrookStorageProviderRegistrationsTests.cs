using System;
using System.Collections.Generic;
using System.Linq;

using Azure.Storage.Blobs;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Mapping;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;
using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime;
using Mississippi.Hosting.Runtime.Abstractions;

using Moq;

using Orleans.Hosting;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Unit tests for Brooks Cosmos runtime composition and provider registrations.
/// </summary>
public sealed class BrookStorageProviderRegistrationsTests
{
    private const string BlobConnectionString = "UseDevelopmentStorage=true";

    private const string CosmosConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM=;";

    private const string CustomCosmosKey = "custom-cosmos";

    private static void AddHostOwnedClients(
        IServiceCollection services,
        string cosmosKey = BrookCosmosDefaults.CosmosClientServiceKey
    )
    {
        services.AddKeyedSingleton<CosmosClient>(
            cosmosKey,
            (_, _) => throw new InvalidOperationException("The host-owned Cosmos client factory should remain lazy."));
        services.AddKeyedSingleton<BlobServiceClient>(
            BrookCosmosDefaults.BlobLockingServiceKey,
            (_, _) => throw new InvalidOperationException("The host-owned Blob client factory should remain lazy."));
    }

    private static ISiloBuilder CreateSilo(
        IServiceCollection services
    )
    {
        Mock<ISiloBuilder> silo = new();
        silo.SetupGet(builder => builder.Services).Returns(services);
        silo.SetupGet(builder => builder.Configuration).Returns(new ConfigurationBuilder().Build());
        return silo.Object;
    }

    /// <summary>
    ///     Verifies the staged registration keeps the provider graph, mappers, hosted initializer, and three
    ///     independent provider interface descriptors.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderRegistersServicesAndMappers()
    {
        ServiceCollection services = new();
        AddHostOwnedClients(services);
        CreateSilo(services).UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider());
        List<ServiceDescriptor> descriptors = services.ToList();
        List<Type> serviceTypes = descriptors.Select(descriptor => descriptor.ServiceType).ToList();
        Type[] expectedCoreServices =
        {
            typeof(IBrookStorageProvider),
            typeof(IBrookRecoveryService),
            typeof(IEventBrookReader),
            typeof(IEventBrookWriter),
            typeof(ICosmosRepository),
            typeof(IDistributedLockManager),
            typeof(IBlobLeaseClientFactory),
            typeof(IBatchSizeEstimator),
            typeof(IRetryPolicy),
        };
        foreach (Type expected in expectedCoreServices)
        {
            Assert.Contains(expected, serviceTypes);
        }

        Assert.Contains(
            descriptors,
            descriptor => (descriptor.ServiceType == typeof(IMapper<EventStorageModel, BrookEvent>)) &&
                          (descriptor.ImplementationType == typeof(EventStorageToEventMapper)));
        Assert.Contains(
            descriptors,
            descriptor => (descriptor.ServiceType == typeof(IMapper<BrookEvent, EventStorageModel>)) &&
                          (descriptor.ImplementationType == typeof(EventToStorageMapper)));
        Assert.Contains(
            descriptors,
            descriptor => (descriptor.ServiceType == typeof(IMapper<CursorDocument, CursorStorageModel>)) &&
                          (descriptor.ImplementationType == typeof(CursorDocumentToStorageMapper)));
        Assert.Contains(
            descriptors,
            descriptor => (descriptor.ServiceType == typeof(IMapper<EventDocument, EventStorageModel>)) &&
                          (descriptor.ImplementationType == typeof(EventDocumentToStorageMapper)));
        Assert.Contains(
            descriptors,
            descriptor => descriptor.IsKeyedService &&
                          (descriptor.ServiceType == typeof(Container)) &&
                          Equals(descriptor.ServiceKey, BrookCosmosDefaults.CosmosContainerServiceKey));
        Assert.Equal(1, descriptors.Count(descriptor => descriptor.ServiceType == typeof(IHostedService)));
        Assert.Equal(1, descriptors.Count(descriptor => descriptor.ServiceType == typeof(IBrookStorageProvider)));
        Assert.Equal(1, descriptors.Count(descriptor => descriptor.ServiceType == typeof(IBrookStorageReader)));
        Assert.Equal(1, descriptors.Count(descriptor => descriptor.ServiceType == typeof(IBrookStorageWriter)));
        Assert.All(
            descriptors.Where(descriptor => (descriptor.ServiceType == typeof(IBrookStorageProvider)) ||
                                            (descriptor.ServiceType == typeof(IBrookStorageReader)) ||
                                            (descriptor.ServiceType == typeof(IBrookStorageWriter))),
            descriptor =>
            {
                Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
                Assert.Equal(typeof(BrookStorageProvider), descriptor.ImplementationType);
            });
        Assert.Equal(
            1,
            descriptors.Count(descriptor => descriptor.IsKeyedService &&
                                            (descriptor.ServiceType == typeof(CosmosClient)) &&
                                            Equals(descriptor.ServiceKey, BrookCosmosDefaults.CosmosClientServiceKey)));
        Assert.Equal(
            1,
            descriptors.Count(descriptor => descriptor.IsKeyedService &&
                                            (descriptor.ServiceType == typeof(BlobServiceClient)) &&
                                            Equals(descriptor.ServiceKey, BrookCosmosDefaults.BlobLockingServiceKey)));
        Assert.DoesNotContain(
            descriptors,
            descriptor => !descriptor.IsKeyedService &&
                          ((descriptor.ServiceType == typeof(CosmosClient)) ||
                           (descriptor.ServiceType == typeof(BlobServiceClient))));
    }

    /// <summary>Verifies DI AnyKey registrations satisfy the host-owned keyed client contract.</summary>
    [Fact]
    public void AnyKeyClientRegistrationsSatisfyHostOwnedValidation()
    {
        ServiceCollection services = new();
        services.AddKeyedSingleton<CosmosClient>(
            KeyedService.AnyKey,
            (_, _) => throw new InvalidOperationException("The AnyKey Cosmos client factory should remain lazy."));
        services.AddKeyedSingleton<BlobServiceClient>(
            KeyedService.AnyKey,
            (_, _) => throw new InvalidOperationException("The AnyKey Blob client factory should remain lazy."));
        CreateSilo(services)
            .UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider(cosmos =>
                cosmos.CosmosClientServiceKey = CustomCosmosKey));
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(
            CustomCosmosKey,
            provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value.CosmosClientServiceKey);
    }

    /// <summary>Verifies the configuration overload binds every scalar option before staging.</summary>
    [Fact]
    public void ConfigurationOverloadBindsOptions()
    {
        ServiceCollection services = new();
        AddHostOwnedClients(services, CustomCosmosKey);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [nameof(BrookStorageOptions.ContainerId)] = "events",
                    [nameof(BrookStorageOptions.CosmosClientServiceKey)] = CustomCosmosKey,
                    [nameof(BrookStorageOptions.DatabaseId)] = "database",
                    [nameof(BrookStorageOptions.LeaseDurationSeconds)] = "30",
                    [nameof(BrookStorageOptions.LeaseRenewalThresholdSeconds)] = "10",
                    [nameof(BrookStorageOptions.LockContainerName)] = "locks",
                    [nameof(BrookStorageOptions.MaxEventsPerBatch)] = "25",
                    [nameof(BrookStorageOptions.MaxRequestSizeBytes)] = "10000",
                    [nameof(BrookStorageOptions.QueryBatchSize)] = "-1",
                })
            .Build();
        CreateSilo(services).UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider(configuration));
        using ServiceProvider provider = services.BuildServiceProvider();
        BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;
        Assert.Equal("events", options.ContainerId);
        Assert.Equal(CustomCosmosKey, options.CosmosClientServiceKey);
        Assert.Equal("database", options.DatabaseId);
        Assert.Equal(30, options.LeaseDurationSeconds);
        Assert.Equal(10, options.LeaseRenewalThresholdSeconds);
        Assert.Equal(25, options.MaxEventsPerBatch);
        Assert.Equal(10_000, options.MaxRequestSizeBytes);
        Assert.Equal(-1, options.QueryBatchSize);
    }

    /// <summary>Verifies connection-string configuration mode binds options and uses the final client key.</summary>
    [Fact]
    public void ConnectionStringsAndConfigurationBindOptions()
    {
        ServiceCollection services = new();
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [nameof(BrookStorageOptions.CosmosClientServiceKey)] = CustomCosmosKey,
                    [nameof(BrookStorageOptions.DatabaseId)] = "database",
                })
            .Build();
        CreateSilo(services)
            .UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider(
                CosmosConnectionString,
                BlobConnectionString,
                configuration));
        using ServiceProvider provider = services.BuildServiceProvider();
        BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;
        Assert.Equal(CustomCosmosKey, options.CosmosClientServiceKey);
        Assert.Equal("database", options.DatabaseId);
        Assert.Contains(
            services,
            descriptor => descriptor.IsKeyedService &&
                          (descriptor.ServiceType == typeof(CosmosClient)) &&
                          Equals(descriptor.ServiceKey, CustomCosmosKey));
    }

    /// <summary>
    ///     Verifies connection-string mode registers lazy keyed factories under the final selected Cosmos key.
    /// </summary>
    [Fact]
    public void ConnectionStringsUseLazyFactoriesWithFinalKey()
    {
        ServiceCollection services = new();
        CreateSilo(services)
            .UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider(
                CosmosConnectionString,
                BlobConnectionString,
                cosmos => cosmos.CosmosClientServiceKey = CustomCosmosKey));
        ServiceDescriptor cosmosDescriptor = Assert.Single(
            services,
            descriptor => descriptor.IsKeyedService &&
                          (descriptor.ServiceType == typeof(CosmosClient)) &&
                          Equals(descriptor.ServiceKey, CustomCosmosKey));
        ServiceDescriptor blobDescriptor = Assert.Single(
            services,
            descriptor => descriptor.IsKeyedService &&
                          (descriptor.ServiceType == typeof(BlobServiceClient)) &&
                          Equals(descriptor.ServiceKey, BrookCosmosDefaults.BlobLockingServiceKey));
        Assert.NotNull(cosmosDescriptor.KeyedImplementationFactory);
        Assert.NotNull(blobDescriptor.KeyedImplementationFactory);
        Assert.DoesNotContain(
            services,
            descriptor => !descriptor.IsKeyedService &&
                          ((descriptor.ServiceType == typeof(CosmosClient)) ||
                           (descriptor.ServiceType == typeof(BlobServiceClient))));
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(
            CustomCosmosKey,
            provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value.CosmosClientServiceKey);
    }

    /// <summary>Verifies a custom final Cosmos key succeeds only when the host supplies that keyed client.</summary>
    [Fact]
    public void CustomCosmosClientKeyMustMatchFinalHostRegistration()
    {
        ServiceCollection missingServices = new();
        AddHostOwnedClients(missingServices);
        CreateSilo(missingServices)
            .UseMississippi(runtime =>
                runtime.AddCosmosBrookStorageProvider(cosmos => cosmos.CosmosClientServiceKey = CustomCosmosKey));
        using (ServiceProvider missingProvider = missingServices.BuildServiceProvider())
        {
            OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
                missingProvider.GetRequiredService<IOptions<BrookStorageOptions>>().Value);
            Assert.Contains(
                BrookStorageBuilderDiagnosticCodes.CosmosClientRegistrationRequired,
                exception.Message,
                StringComparison.Ordinal);
        }

        ServiceCollection matchingServices = new();
        AddHostOwnedClients(matchingServices, CustomCosmosKey);
        CreateSilo(matchingServices)
            .UseMississippi(runtime =>
                runtime.AddCosmosBrookStorageProvider(cosmos => cosmos.CosmosClientServiceKey = CustomCosmosKey));
        using ServiceProvider matchingProvider = matchingServices.BuildServiceProvider();
        Assert.Equal(
            CustomCosmosKey,
            matchingProvider.GetRequiredService<IOptions<BrookStorageOptions>>().Value.CosmosClientServiceKey);
    }

    /// <summary>Verifies duplicate runtime composition is rejected before the second callback executes.</summary>
    [Fact]
    public void DuplicateCompositionIsRejected()
    {
        ServiceCollection services = new();
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() => CreateSilo(services)
            .UseMississippi(runtime =>
            {
                runtime.AddCosmosBrookStorageProvider();
                runtime.AddCosmosBrookStorageProvider(_ => invoked = true);
            }));
        Assert.Equal(
            BrookStorageBuilderDiagnosticCodes.DuplicateComposition,
            Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
    }

    /// <summary>
    ///     Verifies host-owned clients may be added by a later staged callback and are found at options resolution.
    /// </summary>
    [Fact]
    public void HostOwnedClientsCanBeAddedByLaterStagedCallback()
    {
        ServiceCollection services = new();
        ISiloBuilder silo = CreateSilo(services);
        silo.UseMississippi(runtime =>
        {
            runtime.AddCosmosBrookStorageProvider(cosmos => cosmos.CosmosClientServiceKey = CustomCosmosKey);
            runtime.ConfigureSilo(staged =>
            {
                staged.Services.AddKeyedSingleton<CosmosClient>(
                    CustomCosmosKey,
                    (_, _) => throw new InvalidOperationException(
                        "The staged Cosmos client factory should remain lazy."));
                staged.Services.AddKeyedSingleton<BlobServiceClient>(
                    BrookCosmosDefaults.BlobLockingServiceKey,
                    (_, _) => throw new InvalidOperationException(
                        "The staged Blob client factory should remain lazy."));
            });
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;
        Assert.Equal(CustomCosmosKey, options.CosmosClientServiceKey);
    }

    /// <summary>
    ///     Verifies host-owned clients remain host-owned while every configured scalar option is snapshotted.
    /// </summary>
    [Fact]
    public void HostOwnedCompositionSnapshotsOptionsWithoutCreatingClients()
    {
        ServiceCollection services = new();
        AddHostOwnedClients(services, CustomCosmosKey);
        CreateSilo(services)
            .UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider(cosmos =>
            {
                cosmos.ContainerId = "events";
                cosmos.CosmosClientServiceKey = CustomCosmosKey;
                cosmos.DatabaseId = "database";
                cosmos.LeaseDurationSeconds = 30;
                cosmos.LeaseRenewalThresholdSeconds = 10;
                cosmos.LockContainerName = "locks";
                cosmos.MaxEventsPerBatch = 25;
                cosmos.MaxRequestSizeBytes = 10_000;
                cosmos.QueryBatchSize = -1;
            }));
        using ServiceProvider provider = services.BuildServiceProvider();
        BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;
        Assert.Equal("events", options.ContainerId);
        Assert.Equal(CustomCosmosKey, options.CosmosClientServiceKey);
        Assert.Equal("database", options.DatabaseId);
        Assert.Equal(30, options.LeaseDurationSeconds);
        Assert.Equal(10, options.LeaseRenewalThresholdSeconds);
        Assert.Equal("locks", options.LockContainerName);
        Assert.Equal(25, options.MaxEventsPerBatch);
        Assert.Equal(10_000, options.MaxRequestSizeBytes);
        Assert.Equal(-1, options.QueryBatchSize);
        Assert.Equal(
            1,
            services.Count(descriptor => descriptor.IsKeyedService &&
                                         (descriptor.ServiceType == typeof(CosmosClient)) &&
                                         Equals(descriptor.ServiceKey, CustomCosmosKey)));
    }

    /// <summary>Verifies invalid options fail the nested callback with their specific stable diagnostic.</summary>
    [Fact]
    public void InvalidOptionsFailWithSpecificDiagnostic()
    {
        ServiceCollection services = new();
        AddHostOwnedClients(services);
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() => CreateSilo(services)
            .UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider(cosmos => cosmos.QueryBatchSize = 0)));
        Assert.Equal(
            BrookStorageBuilderDiagnosticCodes.QueryBatchSizeInvalid,
            Assert.Single(exception.Diagnostics).Code);
    }

    /// <summary>Verifies missing final keyed clients fail with distinct diagnostics during options resolution.</summary>
    [Fact]
    public void MissingHostOwnedClientsFailWithDistinctDiagnostics()
    {
        ServiceCollection services = new();
        CreateSilo(services).UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider());
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value);
        Assert.Contains(
            BrookStorageBuilderDiagnosticCodes.CosmosClientRegistrationRequired,
            exception.Message,
            StringComparison.Ordinal);
        Assert.Contains(
            BrookStorageBuilderDiagnosticCodes.BlobClientRegistrationRequired,
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>Verifies null public arguments fail at the runtime registration boundary.</summary>
    [Fact]
    public void NullArgumentsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BrookStorageProviderRegistrations.AddCosmosBrookStorageProvider(null!));
        Assert.Throws<ArgumentNullException>(() =>
            BrookStorageProviderRegistrations.AddCosmosBrookStorageProvider(null!, (IConfiguration)null!));
        Assert.Throws<ArgumentException>(() =>
            new Mock<IRuntimeBuilder>().Object.AddCosmosBrookStorageProvider(" ", BlobConnectionString));
    }

    /// <summary>Verifies options changed after staging are rejected by startup validation with a specific code.</summary>
    [Fact]
    public void StartupOptionsValidationUsesSpecificDiagnostic()
    {
        ServiceCollection services = new();
        AddHostOwnedClients(services);
        CreateSilo(services).UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider());
        services.PostConfigure<BrookStorageOptions>(options =>
            options.MaxRequestSizeBytes = BatchSizeEstimator.BatchOverheadBytes);
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value);
        Assert.Contains(
            BrookStorageBuilderDiagnosticCodes.MaxRequestSizeInvalid,
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>Verifies a successful public callback closes its nested builder and attached root.</summary>
    [Fact]
    public void SuccessfulPublicCallbackClosesNestedBuilderAndRoot()
    {
        ServiceCollection services = new();
        AddHostOwnedClients(services);
        CosmosBrookStorageBuilder? capturedBuilder = null;
        IRuntimeBuilder? capturedRoot = null;
        ISiloBuilder silo = CreateSilo(services);
        silo.UseMississippi(runtime =>
        {
            capturedRoot = runtime;
            runtime.AddCosmosBrookStorageProvider(cosmos => capturedBuilder = cosmos);
        });
        Assert.NotNull(capturedBuilder);
        Assert.Equal(
            BrookStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
            Assert.Single(capturedBuilder.Validate()).Code);
        BuilderValidationException lateBuilderMutation = Assert.Throws<BuilderValidationException>(() =>
            capturedBuilder.DatabaseId = "late");
        Assert.Equal(
            BrookStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
            Assert.Single(lateBuilderMutation.Diagnostics).Code);
        Assert.NotNull(capturedRoot);
        BuilderValidationException lateRootComposition = Assert.Throws<BuilderValidationException>(() =>
            capturedRoot.AddCosmosBrookStorageProvider());
        Assert.Equal(
            BuilderDiagnosticCodes.BuilderAlreadyAttached,
            Assert.Single(lateRootComposition.Diagnostics).Code);
    }

    /// <summary>Verifies a throwing public callback still closes its nested builder.</summary>
    [Fact]
    public void ThrowingPublicCallbackClosesNestedBuilder()
    {
        ServiceCollection services = new();
        CosmosBrookStorageBuilder? capturedBuilder = null;
        InvalidOperationException expected = new("Configuration failed.");
        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() => CreateSilo(services)
            .UseMississippi(runtime => runtime.AddCosmosBrookStorageProvider(cosmos =>
            {
                capturedBuilder = cosmos;
                throw expected;
            })));
        Assert.Same(expected, actual);
        Assert.NotNull(capturedBuilder);
        Assert.Equal(
            BrookStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
            Assert.Single(capturedBuilder.Validate()).Code);
        Assert.Throws<BuilderValidationException>(() => capturedBuilder.DatabaseId = "late");
    }
}