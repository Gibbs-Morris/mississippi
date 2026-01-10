using System;
using System.Collections.Generic;
using System.Linq;

using Allure.Xunit.Attributes;

using Azure.Storage.Blobs;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Cosmos.Abstractions.Retry;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;
using Mississippi.EventSourcing.Brooks.Cosmos.Batching;
using Mississippi.EventSourcing.Brooks.Cosmos.Locking;
using Mississippi.EventSourcing.Brooks.Cosmos.Mapping;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.L0Tests;

/// <summary>
///     Unit tests for BrookStorageProviderRegistrations extension methods.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Cosmos")]
[AllureSubSuite("Storage Provider Registrations")]
public sealed class BrookStorageProviderRegistrationsTests
{
    /// <summary>
    ///     Verifies core services and public abstractions are registered.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderRegistersServicesAndMappers()
    {
        ServiceCollection services = new();
        services.AddCosmosBrookStorageProvider(options =>
        {
            options.DatabaseId = "db";
            options.LockContainerName = "locks";
        });

        // The no-arg overload registers the brook provider and related types but does not
        // register SDK clients (those are only added by overloads that accept connection strings).
        // Assert by inspecting service descriptors to avoid resolving services that have
        // constructor dependencies (for example the lock manager requires a BlobServiceClient).
        List<ServiceDescriptor> descriptors = services.ToList();
        List<Type> serviceTypes = descriptors.Select(sd => sd.ServiceType).ToList();
        Type[] expectedCoreServices =
        {
            typeof(IBrookStorageProvider),
            typeof(IBrookRecoveryService),
            typeof(IEventBrookReader),
            typeof(IEventBrookAppender),
            typeof(ICosmosRepository),
            typeof(IDistributedLockManager),
            typeof(IBlobLeaseClientFactory),
            typeof(IBatchSizeEstimator),
            typeof(IRetryPolicy),
            typeof(Container),
        };
        foreach (Type expected in expectedCoreServices)
        {
            Assert.Contains(expected, serviceTypes);
        }

        Assert.Contains(
            descriptors,
            sd => (sd.ServiceType == typeof(IMapper<EventStorageModel, BrookEvent>)) &&
                  (sd.ImplementationType == typeof(EventStorageToEventMapper)));
        Assert.Contains(
            descriptors,
            sd => (sd.ServiceType == typeof(IMapper<BrookEvent, EventStorageModel>)) &&
                  (sd.ImplementationType == typeof(EventToStorageMapper)));
        Assert.Contains(
            descriptors,
            sd => (sd.ServiceType == typeof(IMapper<CursorDocument, CursorStorageModel>)) &&
                  (sd.ImplementationType == typeof(CursorDocumentToStorageMapper)));
        Assert.Contains(
            descriptors,
            sd => (sd.ServiceType == typeof(IMapper<EventDocument, EventStorageModel>)) &&
                  (sd.ImplementationType == typeof(EventDocumentToStorageMapper)));
        Assert.Equal(1, descriptors.Count(sd => sd.ServiceType == typeof(IHostedService)));
        Assert.DoesNotContain(typeof(CosmosClient), serviceTypes);
        Assert.DoesNotContain(typeof(BlobServiceClient), serviceTypes);
    }

    /// <summary>
    ///     Verifies configuration binding overload binds BrookStorageOptions from IConfiguration.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderWithConfigurationBindsOptions()
    {
        Dictionary<string, string?> inMemory = new()
        {
            ["DatabaseId"] = "confdb",
            ["LockContainerName"] = "confblobs",
        };
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        ServiceCollection services = new();

        // Use the IConfiguration-only overload so we don't register SDK clients here.
        services.AddCosmosBrookStorageProvider(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<BrookStorageOptions> opts = provider.GetRequiredService<IOptions<BrookStorageOptions>>();
        Assert.Equal("confdb", opts.Value.DatabaseId);
        Assert.Equal("confblobs", opts.Value.LockContainerName);
    }

    /// <summary>
    ///     Verifies configureOptions overload populates IOptions&lt;BrookStorageOptions&gt;.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderWithConfigureOptionsAppliesOptions()
    {
        ServiceCollection services = new();
        services.AddCosmosBrookStorageProvider(options => { options.DatabaseId = "mydb"; });
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<BrookStorageOptions> opts = provider.GetRequiredService<IOptions<BrookStorageOptions>>();
        Assert.Equal("mydb", opts.Value.DatabaseId);
    }

    /// <summary>
    ///     Verifies that the overload accepting both connection strings and IConfiguration binds options and registers
    ///     clients.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderWithConnectionStringsAndConfigurationBindsAndRegisters()
    {
        ServiceCollection services = new();
        IConfigurationRoot cfg = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["DatabaseId"] = "cfgDb",
                    ["LockContainerName"] = "cfgLocks",
                })
            .Build();
        services.AddCosmosBrookStorageProvider(
            "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM=;",
            "UseDevelopmentStorage=true",
            cfg);

        // Assert by descriptor types to avoid creating real SDK clients
        List<Type> serviceTypes = services.Select(sd => sd.ServiceType).ToList();
        Assert.Contains(typeof(CosmosClient), serviceTypes);
        Assert.Contains(typeof(BlobServiceClient), serviceTypes);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<BrookStorageOptions> opts = provider.GetRequiredService<IOptions<BrookStorageOptions>>();
        Assert.Equal("cfgDb", opts.Value.DatabaseId);
        Assert.Equal("cfgLocks", opts.Value.LockContainerName);
    }

    /// <summary>
    ///     Ensures Cosmos and Blob clients are registered when connection strings overload is used.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderWithConnectionStringsRegistersClients()
    {
        ServiceCollection services = new();
        services.AddCosmosBrookStorageProvider(
            "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM=;",
            "UseDevelopmentStorage=true",
            configureOptions: null);

        // Avoid resolving the real SDK clients (their constructors validate keys).
        // Instead assert that the service descriptors for the SDK clients were added.
        List<Type> serviceTypes2 = services.Select(sd => sd.ServiceType).ToList();
        Assert.Contains(typeof(CosmosClient), serviceTypes2);
        Assert.Contains(typeof(BlobServiceClient), serviceTypes2);
    }
}