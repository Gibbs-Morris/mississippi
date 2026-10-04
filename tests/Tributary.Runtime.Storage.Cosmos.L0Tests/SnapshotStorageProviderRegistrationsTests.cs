using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;
using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime;
using Mississippi.Hosting.Runtime.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Mapping;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;

using Moq;

using Orleans.Hosting;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests for Snapshot Cosmos runtime composition and provider registrations.
/// </summary>
public sealed class SnapshotStorageProviderRegistrationsTests
{
    private const string CosmosConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM=;";

    private const string CustomCosmosKey = "custom-cosmos";

    private static void AddHostOwnedClient(
        IServiceCollection services,
        string key = SnapshotCosmosDefaults.CosmosClientServiceKey
    ) =>
        services.AddKeyedSingleton<CosmosClient>(
            key,
            (_, _) => throw new InvalidOperationException("The host-owned Cosmos client factory should remain lazy."));

    private static void ComposeSnapshot(
        IServiceCollection services,
        Action<CosmosSnapshotStorageBuilder>? configure = null
    ) =>
        CreateSilo(services).UseMississippi(runtime => runtime.AddCosmosSnapshotStorageProvider(configure));

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
    ///     Verifies the staged registration keeps the Snapshot graph, all five mappers, initializer, and provider roles.
    /// </summary>
    [Fact]
    public void AddCosmosSnapshotStorageProviderRegistersServicesAndMappers()
    {
        ServiceCollection services = new();
        AddHostOwnedClient(services);
        ComposeSnapshot(services);
        List<ServiceDescriptor> descriptors = services.ToList();
        Type[] expectedCoreServices =
        {
            typeof(ISnapshotContainerOperations),
            typeof(ISnapshotCosmosRepository),
            typeof(IRetryPolicy),
            typeof(ISnapshotStorageProvider),
            typeof(ISnapshotStorageReader),
            typeof(ISnapshotStorageWriter),
        };
        foreach (Type expected in expectedCoreServices)
        {
            Assert.Contains(expected, descriptors.Select(descriptor => descriptor.ServiceType));
        }

        Assert.Contains(
            descriptors,
            descriptor => (descriptor.ServiceType == typeof(IMapper<SnapshotDocument, SnapshotStorageModel>)) &&
                          (descriptor.ImplementationType == typeof(SnapshotDocumentToStorageMapper)));
        Assert.Contains(
            descriptors,
            descriptor => (descriptor.ServiceType == typeof(IMapper<SnapshotStorageModel, SnapshotEnvelope>)) &&
                          (descriptor.ImplementationType == typeof(SnapshotStorageToEnvelopeMapper)));
        Assert.Contains(
            descriptors,
            descriptor => (descriptor.ServiceType == typeof(IMapper<SnapshotWriteModel, SnapshotStorageModel>)) &&
                          (descriptor.ImplementationType == typeof(SnapshotWriteModelToStorageMapper)));
        Assert.Contains(
            descriptors,
            descriptor => (descriptor.ServiceType == typeof(IMapper<SnapshotStorageModel, SnapshotDocument>)) &&
                          (descriptor.ImplementationType == typeof(SnapshotStorageToDocumentMapper)));
        Assert.Contains(
            descriptors,
            descriptor => (descriptor.ServiceType == typeof(IMapper<SnapshotDocument, SnapshotEnvelope>)) &&
                          (descriptor.ImplementationType == typeof(SnapshotDocumentToEnvelopeMapper)));
        Assert.Contains(
            descriptors,
            descriptor => descriptor.IsKeyedService &&
                          (descriptor.ServiceType == typeof(Container)) &&
                          Equals(descriptor.ServiceKey, SnapshotCosmosDefaults.CosmosContainerServiceKey));
        ServiceDescriptor provider = Assert.Single(
            descriptors,
            descriptor => descriptor.ServiceType == typeof(ISnapshotStorageProvider));
        Assert.Equal(ServiceLifetime.Singleton, provider.Lifetime);
        Assert.Equal(typeof(SnapshotStorageProvider), provider.ImplementationType);
        ServiceDescriptor reader = Assert.Single(
            descriptors,
            descriptor => descriptor.ServiceType == typeof(ISnapshotStorageReader));
        ServiceDescriptor writer = Assert.Single(
            descriptors,
            descriptor => descriptor.ServiceType == typeof(ISnapshotStorageWriter));
        Assert.Equal(ServiceLifetime.Singleton, reader.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, writer.Lifetime);
        Assert.NotNull(reader.ImplementationFactory);
        Assert.NotNull(writer.ImplementationFactory);
        Assert.Equal(1, descriptors.Count(descriptor => descriptor.ServiceType == typeof(IHostedService)));
        Assert.Equal(
            1,
            descriptors.Count(descriptor => descriptor.IsKeyedService &&
                                            (descriptor.ServiceType == typeof(CosmosClient)) &&
                                            Equals(
                                                descriptor.ServiceKey,
                                                SnapshotCosmosDefaults.CosmosClientServiceKey)));
        Assert.DoesNotContain(
            descriptors,
            descriptor => !descriptor.IsKeyedService && (descriptor.ServiceType == typeof(CosmosClient)));
    }

    /// <summary>
    ///     Verifies aliases follow the last unkeyed provider descriptor while keyed registrations remain independent.
    /// </summary>
    [Fact]
    public void AliasesUseEffectiveLastUnkeyedProviderDescriptor()
    {
        ServiceCollection services = new();
        FakeSnapshotStorageProvider firstProvider = new();
        services.AddSingleton<ISnapshotStorageProvider>(firstProvider);
        services.AddScoped<ISnapshotStorageProvider, FakeSnapshotStorageProvider>();
        FakeSnapshotStorageProvider keyedProvider = new();
        services.AddKeyedSingleton<ISnapshotStorageProvider>("keyed", keyedProvider);
        AddHostOwnedClient(services);
        ComposeSnapshot(services);
        ServiceDescriptor reader = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ISnapshotStorageReader));
        ServiceDescriptor writer = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ISnapshotStorageWriter));
        Assert.Equal(ServiceLifetime.Scoped, reader.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, writer.Lifetime);
        Assert.Contains(
            services,
            candidate => candidate.IsKeyedService &&
                         (candidate.ServiceType == typeof(ISnapshotStorageProvider)) &&
                         Equals(candidate.ServiceKey, "keyed"));
        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true,
            });
        using IServiceScope scope = provider.CreateScope();
        ISnapshotStorageProvider effectiveProvider =
            scope.ServiceProvider.GetRequiredService<ISnapshotStorageProvider>();
        Assert.Same(keyedProvider, scope.ServiceProvider.GetRequiredKeyedService<ISnapshotStorageProvider>("keyed"));
        Assert.NotSame(firstProvider, effectiveProvider);
        Assert.NotSame(keyedProvider, effectiveProvider);
        Assert.Same(effectiveProvider, scope.ServiceProvider.GetRequiredService<ISnapshotStorageReader>());
        Assert.Same(effectiveProvider, scope.ServiceProvider.GetRequiredService<ISnapshotStorageWriter>());
    }

    /// <summary>
    ///     Verifies a keyed AnyKey registration satisfies a callback-selected client key.
    /// </summary>
    [Fact]
    public void AnyKeyClientRegistrationSatisfiesHostOwnedValidation()
    {
        ServiceCollection services = new();
        services.AddKeyedSingleton<CosmosClient>(
            KeyedService.AnyKey,
            (_, _) => throw new InvalidOperationException("The AnyKey Cosmos client factory should remain lazy."));
        ComposeSnapshot(services, snapshot => snapshot.CosmosClientServiceKey = CustomCosmosKey);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(
            CustomCosmosKey,
            provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value.CosmosClientServiceKey);
    }

    /// <summary>
    ///     Verifies a configuration overload binds every scalar option before staging.
    /// </summary>
    [Fact]
    public void ConfigurationOverloadBindsAllOptions()
    {
        ServiceCollection services = new();
        AddHostOwnedClient(services, CustomCosmosKey);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [nameof(SnapshotStorageOptions.ContainerId)] = "configured-snapshots",
                    [nameof(SnapshotStorageOptions.CosmosClientServiceKey)] = CustomCosmosKey,
                    [nameof(SnapshotStorageOptions.DatabaseId)] = "configured-database",
                    [nameof(SnapshotStorageOptions.QueryBatchSize)] = "-1",
                })
            .Build();
        CreateSilo(services).UseMississippi(runtime => runtime.AddCosmosSnapshotStorageProvider(configuration));
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
        Assert.Equal("configured-snapshots", options.ContainerId);
        Assert.Equal(CustomCosmosKey, options.CosmosClientServiceKey);
        Assert.Equal("configured-database", options.DatabaseId);
        Assert.Equal(-1, options.QueryBatchSize);
    }

    /// <summary>
    ///     Verifies connection-string and configuration overloads use the final selected Cosmos client key.
    /// </summary>
    [Fact]
    public void ConnectionStringAndConfigurationUseFinalKey()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [nameof(SnapshotStorageOptions.CosmosClientServiceKey)] = CustomCosmosKey,
                    [nameof(SnapshotStorageOptions.DatabaseId)] = "configured-database",
                })
            .Build();
        CreateSilo(services)
            .UseMississippi(runtime => runtime.AddCosmosSnapshotStorageProvider(CosmosConnectionString, configuration));
        ServiceDescriptor cosmosDescriptor = Assert.Single(
            services,
            descriptor => descriptor.IsKeyedService &&
                          (descriptor.ServiceType == typeof(CosmosClient)) &&
                          Equals(descriptor.ServiceKey, CustomCosmosKey));
        Assert.NotNull(cosmosDescriptor.KeyedImplementationFactory);
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.IsKeyedService &&
                          (descriptor.ServiceType == typeof(CosmosClient)) &&
                          Equals(descriptor.ServiceKey, SnapshotCosmosDefaults.CosmosClientServiceKey));
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
        Assert.Equal(CustomCosmosKey, options.CosmosClientServiceKey);
        Assert.Equal("configured-database", options.DatabaseId);
    }

    /// <summary>
    ///     Verifies the connection-string callback overload lazily registers under a callback-selected key.
    /// </summary>
    [Fact]
    public void ConnectionStringCallbackUsesLazyFactoryWithFinalKey()
    {
        ServiceCollection services = new();
        CreateSilo(services)
            .UseMississippi(runtime => runtime.AddCosmosSnapshotStorageProvider(
                CosmosConnectionString,
                snapshot => snapshot.CosmosClientServiceKey = CustomCosmosKey));
        ServiceDescriptor descriptor = Assert.Single(
            services,
            candidate => candidate.IsKeyedService &&
                         (candidate.ServiceType == typeof(CosmosClient)) &&
                         Equals(candidate.ServiceKey, CustomCosmosKey));
        Assert.NotNull(descriptor.KeyedImplementationFactory);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(
            CustomCosmosKey,
            provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value.CosmosClientServiceKey);
    }

    /// <summary>
    ///     Verifies that an existing database/container initializer still creates a missing container.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CosmosContainerInitializerCreatesContainerWhenNotFound()
    {
        Mock<ContainerResponse> containerResponse = new();
        Mock<Container> container = new();
        container.Setup(c => c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty, 0));
        Mock<Database> database = new();
        database.Setup(d => d.GetContainer("new-container")).Returns(container.Object);
        database.Setup(d => d.CreateContainerIfNotExistsAsync(
                "new-container",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponse.Object);
        Mock<DatabaseResponse> databaseResponse = new();
        databaseResponse.Setup(r => r.Database).Returns(database.Object);
        Mock<CosmosClient> cosmosClient = new();
        cosmosClient.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                "new-db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseResponse.Object);
        ServiceCollection services = new();
        services.AddKeyedSingleton(SnapshotCosmosDefaults.CosmosClientServiceKey, cosmosClient.Object);
        ComposeSnapshot(
            services,
            snapshot =>
            {
                snapshot.DatabaseId = "new-db";
                snapshot.ContainerId = "new-container";
            });
        using ServiceProvider provider = services.BuildServiceProvider();
        IHostedService initializer = provider.GetServices<IHostedService>()
            .First(service => service.GetType().Name == "CosmosContainerInitializer");
        await initializer.StartAsync(CancellationToken.None);
        database.Verify(
            d => d.CreateContainerIfNotExistsAsync(
                "new-container",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies the initializer creates the configured database and container.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CosmosContainerInitializerCreatesDatabaseAndContainer()
    {
        Mock<ContainerResponse> containerResponse = new();
        containerResponse.Setup(r => r.Resource)
            .Returns(
                new ContainerProperties
                {
                    PartitionKeyPath = "/snapshotPartitionKey",
                });
        Mock<Container> container = new();
        container.Setup(c => c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponse.Object);
        Mock<Database> database = new();
        database.Setup(d => d.GetContainer("test-container")).Returns(container.Object);
        database.Setup(d => d.CreateContainerIfNotExistsAsync(
                "test-container",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponse.Object);
        Mock<DatabaseResponse> databaseResponse = new();
        databaseResponse.Setup(r => r.Database).Returns(database.Object);
        Mock<CosmosClient> cosmosClient = new();
        cosmosClient.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                "test-db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseResponse.Object);
        ServiceCollection services = new();
        services.AddKeyedSingleton(SnapshotCosmosDefaults.CosmosClientServiceKey, cosmosClient.Object);
        ComposeSnapshot(
            services,
            snapshot =>
            {
                snapshot.DatabaseId = "test-db";
                snapshot.ContainerId = "test-container";
            });
        using ServiceProvider provider = services.BuildServiceProvider();
        IHostedService initializer = provider.GetServices<IHostedService>()
            .First(service => service.GetType().Name == "CosmosContainerInitializer");
        await initializer.StartAsync(CancellationToken.None);
        cosmosClient.Verify(
            c => c.CreateDatabaseIfNotExistsAsync(
                "test-db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        database.Verify(
            d => d.CreateContainerIfNotExistsAsync(
                "test-container",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies the initializer rejects an existing container with the wrong partition key.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CosmosContainerInitializerRejectsPartitionKeyMismatch()
    {
        Mock<ContainerResponse> containerResponse = new();
        containerResponse.Setup(r => r.Resource)
            .Returns(
                new ContainerProperties
                {
                    PartitionKeyPath = "/wrongPartitionKey",
                });
        Mock<Container> container = new();
        container.Setup(c => c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponse.Object);
        Mock<Database> database = new();
        database.Setup(d => d.GetContainer("existing-container")).Returns(container.Object);
        Mock<DatabaseResponse> databaseResponse = new();
        databaseResponse.Setup(r => r.Database).Returns(database.Object);
        Mock<CosmosClient> cosmosClient = new();
        cosmosClient.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                "existing-db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseResponse.Object);
        ServiceCollection services = new();
        services.AddKeyedSingleton(SnapshotCosmosDefaults.CosmosClientServiceKey, cosmosClient.Object);
        ComposeSnapshot(
            services,
            snapshot =>
            {
                snapshot.DatabaseId = "existing-db";
                snapshot.ContainerId = "existing-container";
            });
        using ServiceProvider provider = services.BuildServiceProvider();
        IHostedService initializer = provider.GetServices<IHostedService>()
            .First(service => service.GetType().Name == "CosmosContainerInitializer");
        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.StartAsync(CancellationToken.None));
        Assert.Contains("/wrongPartitionKey", exception.Message, StringComparison.Ordinal);
        Assert.Contains("/snapshotPartitionKey", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies the initializer stop operation completes.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task CosmosContainerInitializerStopsSuccessfully()
    {
        Mock<ContainerResponse> containerResponse = new();
        containerResponse.Setup(r => r.Resource)
            .Returns(
                new ContainerProperties
                {
                    PartitionKeyPath = "/snapshotPartitionKey",
                });
        Mock<Container> container = new();
        container.Setup(c => c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponse.Object);
        Mock<Database> database = new();
        database.Setup(d => d.GetContainer("snapshots")).Returns(container.Object);
        database.Setup(d => d.CreateContainerIfNotExistsAsync(
                "snapshots",
                "/snapshotPartitionKey",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(containerResponse.Object);
        Mock<DatabaseResponse> databaseResponse = new();
        databaseResponse.Setup(r => r.Database).Returns(database.Object);
        Mock<CosmosClient> cosmosClient = new();
        cosmosClient.Setup(c => c.CreateDatabaseIfNotExistsAsync(
                "db",
                It.IsAny<int?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseResponse.Object);
        ServiceCollection services = new();
        services.AddKeyedSingleton(SnapshotCosmosDefaults.CosmosClientServiceKey, cosmosClient.Object);
        ComposeSnapshot(services, snapshot => snapshot.DatabaseId = "db");
        using ServiceProvider provider = services.BuildServiceProvider();
        IHostedService initializer = provider.GetServices<IHostedService>()
            .First(service => service.GetType().Name == "CosmosContainerInitializer");
        await initializer.StartAsync(CancellationToken.None);
        Task stopTask = initializer.StopAsync(CancellationToken.None);
        await stopTask;
        Assert.True(stopTask.IsCompletedSuccessfully);
    }

    /// <summary>
    ///     Verifies the default provider, reader, and writer resolve the same instance without contacting Cosmos.
    /// </summary>
    [Fact]
    public void DefaultProviderReaderAndWriterShareOneInstance()
    {
        ServiceCollection services = new();
        services.AddLogging();
        ComposeSnapshot(services);
        services.AddSingleton(Mock.Of<ISnapshotCosmosRepository>());
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotStorageProvider storageProvider = provider.GetRequiredService<ISnapshotStorageProvider>();
        Assert.IsType<SnapshotStorageProvider>(storageProvider);
        Assert.Same(storageProvider, provider.GetRequiredService<ISnapshotStorageReader>());
        Assert.Same(storageProvider, provider.GetRequiredService<ISnapshotStorageWriter>());
    }

    /// <summary>
    ///     Verifies duplicate runtime composition is rejected before the second callback executes.
    /// </summary>
    [Fact]
    public void DuplicateCompositionIsRejected()
    {
        ServiceCollection services = new();
        bool callbackInvoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() => CreateSilo(services)
            .UseMississippi(runtime =>
            {
                runtime.AddCosmosSnapshotStorageProvider();
                runtime.AddCosmosSnapshotStorageProvider(_ => callbackInvoked = true);
            }));
        Assert.Equal(
            SnapshotStorageBuilderDiagnosticCodes.DuplicateComposition,
            Assert.Single(exception.Diagnostics).Code);
        Assert.False(callbackInvoked);
    }

    /// <summary>
    ///     Verifies keyed clients registered by a later staged callback satisfy final options validation.
    /// </summary>
    [Fact]
    public void HostOwnedClientCanBeAddedByLaterStagedCallback()
    {
        ServiceCollection services = new();
        CreateSilo(services)
            .UseMississippi(runtime =>
            {
                runtime.AddCosmosSnapshotStorageProvider(snapshot => snapshot.CosmosClientServiceKey = CustomCosmosKey);
                runtime.ConfigureSilo(staged => staged.Services.AddKeyedSingleton<CosmosClient>(
                    CustomCosmosKey,
                    (_, _) => throw new InvalidOperationException(
                        "The later staged Cosmos client factory should remain lazy.")));
            });
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(
            CustomCosmosKey,
            provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value.CosmosClientServiceKey);
    }

    /// <summary>
    ///     Verifies the nested builder snapshots all four options without creating a host-owned client.
    /// </summary>
    [Fact]
    public void HostOwnedCompositionSnapshotsOptionsWithoutCreatingClient()
    {
        ServiceCollection services = new();
        AddHostOwnedClient(services, CustomCosmosKey);
        ComposeSnapshot(
            services,
            snapshot =>
            {
                snapshot.ContainerId = "snapshots-custom";
                snapshot.CosmosClientServiceKey = CustomCosmosKey;
                snapshot.DatabaseId = "database";
                snapshot.QueryBatchSize = -1;
            });
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
        Assert.Equal("snapshots-custom", options.ContainerId);
        Assert.Equal(CustomCosmosKey, options.CosmosClientServiceKey);
        Assert.Equal("database", options.DatabaseId);
        Assert.Equal(-1, options.QueryBatchSize);
    }

    /// <summary>
    ///     Verifies scalar composition validation uses the specific query diagnostic.
    /// </summary>
    [Fact]
    public void InvalidOptionsFailDuringComposition()
    {
        ServiceCollection services = new();
        AddHostOwnedClient(services);
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            ComposeSnapshot(services, snapshot => snapshot.QueryBatchSize = 0));
        Assert.Equal(
            SnapshotStorageBuilderDiagnosticCodes.QueryBatchSizeInvalid,
            Assert.Single(exception.Diagnostics).Code);
    }

    /// <summary>
    ///     Verifies missing final keyed clients fail with a stable diagnostic during options resolution.
    /// </summary>
    [Fact]
    public void MissingHostOwnedClientFailsDuringOptionsResolution()
    {
        ServiceCollection services = new();
        ComposeSnapshot(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value);
        Assert.Contains(
            SnapshotStorageBuilderDiagnosticCodes.CosmosClientRegistrationRequired,
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies null and blank public arguments fail at the registration boundary.
    /// </summary>
    [Fact]
    public void NullAndBlankArgumentsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SnapshotStorageProviderRegistrations.AddCosmosSnapshotStorageProvider(null!));
        Assert.Throws<ArgumentNullException>(() =>
            new Mock<IRuntimeBuilder>().Object.AddCosmosSnapshotStorageProvider((IConfiguration)null!));
        Assert.Throws<ArgumentException>(() =>
            new Mock<IRuntimeBuilder>().Object.AddCosmosSnapshotStorageProvider(" "));
    }

    /// <summary>
    ///     Verifies scoped aliases share the scoped provider within a scope and resolve a new provider in another scope.
    /// </summary>
    [Fact]
    public void PreRegisteredScopedProviderUsesScopedAliasesPerScope()
    {
        ServiceCollection services = new();
        services.AddScoped<ISnapshotStorageProvider, FakeSnapshotStorageProvider>();
        AddHostOwnedClient(services);
        ComposeSnapshot(services);
        ServiceDescriptor reader = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ISnapshotStorageReader));
        ServiceDescriptor writer = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ISnapshotStorageWriter));
        Assert.Equal(ServiceLifetime.Scoped, reader.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, writer.Lifetime);
        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true,
            });
        using IServiceScope firstScope = provider.CreateScope();
        ISnapshotStorageProvider firstProvider =
            firstScope.ServiceProvider.GetRequiredService<ISnapshotStorageProvider>();
        Assert.Same(firstProvider, firstScope.ServiceProvider.GetRequiredService<ISnapshotStorageReader>());
        Assert.Same(firstProvider, firstScope.ServiceProvider.GetRequiredService<ISnapshotStorageWriter>());
        using IServiceScope secondScope = provider.CreateScope();
        ISnapshotStorageProvider secondProvider =
            secondScope.ServiceProvider.GetRequiredService<ISnapshotStorageProvider>();
        Assert.NotSame(firstProvider, secondProvider);
        Assert.Same(secondProvider, secondScope.ServiceProvider.GetRequiredService<ISnapshotStorageReader>());
        Assert.Same(secondProvider, secondScope.ServiceProvider.GetRequiredService<ISnapshotStorageWriter>());
    }

    /// <summary>
    ///     Verifies an existing singleton custom provider is shared by provider, reader, and writer contracts.
    /// </summary>
    [Fact]
    public void PreRegisteredSingletonProviderIsSharedByAllRoles()
    {
        ServiceCollection services = new();
        FakeSnapshotStorageProvider customProvider = new();
        services.AddSingleton<ISnapshotStorageProvider>(customProvider);
        AddHostOwnedClient(services);
        ComposeSnapshot(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(customProvider, provider.GetRequiredService<ISnapshotStorageProvider>());
        Assert.Same(customProvider, provider.GetRequiredService<ISnapshotStorageReader>());
        Assert.Same(customProvider, provider.GetRequiredService<ISnapshotStorageWriter>());
        ServiceDescriptor descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ISnapshotStorageProvider));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Same(customProvider, descriptor.ImplementationInstance);
    }

    /// <summary>
    ///     Verifies a pre-registered transient provider gives its reader and writer aliases transient lifetimes.
    /// </summary>
    [Fact]
    public void PreRegisteredTransientProviderUsesTransientAliases()
    {
        ServiceCollection services = new();
        services.AddTransient<ISnapshotStorageProvider, FakeSnapshotStorageProvider>();
        AddHostOwnedClient(services);
        ComposeSnapshot(services);
        ServiceDescriptor descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ISnapshotStorageProvider));
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        Assert.Equal(typeof(FakeSnapshotStorageProvider), descriptor.ImplementationType);
        ServiceDescriptor reader = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ISnapshotStorageReader));
        ServiceDescriptor writer = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(ISnapshotStorageWriter));
        Assert.Equal(ServiceLifetime.Transient, reader.Lifetime);
        Assert.Equal(ServiceLifetime.Transient, writer.Lifetime);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotSame(
            provider.GetRequiredService<ISnapshotStorageReader>(),
            provider.GetRequiredService<ISnapshotStorageReader>());
        Assert.NotSame(
            provider.GetRequiredService<ISnapshotStorageWriter>(),
            provider.GetRequiredService<ISnapshotStorageWriter>());
    }

    /// <summary>
    ///     Verifies a custom singleton provider staged before Snapshot Cosmos composition remains the shared provider.
    /// </summary>
    [Fact]
    public void StagedSingletonProviderIsSharedByAllRoles()
    {
        ServiceCollection services = new();
        FakeSnapshotStorageProvider customProvider = new();
        CreateSilo(services)
            .UseMississippi(runtime =>
            {
                runtime.ConfigureSilo(staged => staged.Services.AddSingleton<ISnapshotStorageProvider>(customProvider));
                runtime.AddCosmosSnapshotStorageProvider();
            });
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(customProvider, provider.GetRequiredService<ISnapshotStorageProvider>());
        Assert.Same(customProvider, provider.GetRequiredService<ISnapshotStorageReader>());
        Assert.Same(customProvider, provider.GetRequiredService<ISnapshotStorageWriter>());
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(ISnapshotStorageProvider)));
    }

    /// <summary>
    ///     Verifies a post-composition options mutation is caught during startup options validation.
    /// </summary>
    [Fact]
    public void StartupOptionsValidationUsesSpecificDiagnostic()
    {
        ServiceCollection services = new();
        AddHostOwnedClient(services);
        ComposeSnapshot(services);
        services.PostConfigure<SnapshotStorageOptions>(options => options.QueryBatchSize = 0);
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value);
        Assert.Contains(
            SnapshotStorageBuilderDiagnosticCodes.QueryBatchSizeInvalid,
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies a successful public callback closes its nested builder and attached root.
    /// </summary>
    [Fact]
    public void SuccessfulPublicCallbackClosesNestedBuilderAndRoot()
    {
        ServiceCollection services = new();
        CosmosSnapshotStorageBuilder? capturedBuilder = null;
        IRuntimeBuilder? capturedRoot = null;
        CreateSilo(services)
            .UseMississippi(runtime =>
            {
                capturedRoot = runtime;
                runtime.AddCosmosSnapshotStorageProvider(snapshot => capturedBuilder = snapshot);
            });
        Assert.NotNull(capturedBuilder);
        Assert.Equal(
            SnapshotStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
            Assert.Single(capturedBuilder.Validate()).Code);
        BuilderValidationException lateMutation = Assert.Throws<BuilderValidationException>(() =>
            capturedBuilder.DatabaseId = "late");
        Assert.Equal(
            SnapshotStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
            Assert.Single(lateMutation.Diagnostics).Code);
        Assert.NotNull(capturedRoot);
        BuilderValidationException lateComposition = Assert.Throws<BuilderValidationException>(() =>
            capturedRoot.AddCosmosSnapshotStorageProvider());
        Assert.Equal(BuilderDiagnosticCodes.BuilderAlreadyAttached, Assert.Single(lateComposition.Diagnostics).Code);
    }

    /// <summary>
    ///     Verifies a throwing public callback still closes its nested builder.
    /// </summary>
    [Fact]
    public void ThrowingPublicCallbackClosesNestedBuilder()
    {
        ServiceCollection services = new();
        CosmosSnapshotStorageBuilder? capturedBuilder = null;
        InvalidOperationException expected = new("Configuration failed.");
        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() => CreateSilo(services)
            .UseMississippi(runtime => runtime.AddCosmosSnapshotStorageProvider(snapshot =>
            {
                capturedBuilder = snapshot;
                throw expected;
            })));
        Assert.Same(expected, actual);
        Assert.NotNull(capturedBuilder);
        Assert.Equal(
            SnapshotStorageBuilderDiagnosticCodes.ConfigurationScopeClosed,
            Assert.Single(capturedBuilder.Validate()).Code);
        Assert.Throws<BuilderValidationException>(() => capturedBuilder.DatabaseId = "late");
    }
}