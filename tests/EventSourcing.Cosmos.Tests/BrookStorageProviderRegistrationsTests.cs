using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Xunit;

namespace Mississippi.EventSourcing.Cosmos.Tests;

/// <summary>
/// Unit tests for BrookStorageProviderRegistrations extension methods.
/// </summary>
public class BrookStorageProviderRegistrationsTests
{
    /// <summary>
    /// Verifies core services and public abstractions are registered.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderRegistersServicesAndMappers()
    {
        var services = new ServiceCollection();

        services.AddCosmosBrookStorageProvider(options =>
        {
            options.DatabaseId = "db";
            options.LockContainerName = "locks";
        });

        // The no-arg overload registers the brook provider and related types but does not
        // register SDK clients (those are only added by overloads that accept connection strings).
        // Assert by inspecting service descriptors to avoid resolving services that have
        // constructor dependencies (for example the lock manager requires a BlobServiceClient).
        var serviceTypes = services.Select(sd => sd.ServiceType).ToList();
        Assert.Contains(typeof(Mississippi.EventSourcing.Abstractions.Storage.IBrookStorageProvider), serviceTypes);
        Assert.DoesNotContain(typeof(Microsoft.Azure.Cosmos.CosmosClient), serviceTypes);
        Assert.DoesNotContain(typeof(Azure.Storage.Blobs.BlobServiceClient), serviceTypes);
    }

    /// <summary>
    /// Ensures Cosmos and Blob clients are registered when connection strings overload is used.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderWithConnectionStringsRegistersClients()
    {
        var services = new ServiceCollection();

        services.AddCosmosBrookStorageProvider(
            cosmosConnectionString: "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM=;",
            blobStorageConnectionString: "UseDevelopmentStorage=true",
            configureOptions: null);

        // Avoid resolving the real SDK clients (their constructors validate keys).
        // Instead assert that the service descriptors for the SDK clients were added.
        var serviceTypes2 = services.Select(sd => sd.ServiceType).ToList();
        Assert.Contains(typeof(Microsoft.Azure.Cosmos.CosmosClient), serviceTypes2);
        Assert.Contains(typeof(Azure.Storage.Blobs.BlobServiceClient), serviceTypes2);
    }

    /// <summary>
    /// Verifies configureOptions overload populates IOptions&lt;BrookStorageOptions&gt;.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderWithConfigureOptionsAppliesOptions()
    {
        var services = new ServiceCollection();

        services.AddCosmosBrookStorageProvider(options =>
        {
            options.DatabaseId = "mydb";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<Mississippi.EventSourcing.Cosmos.BrookStorageOptions>>();

        Assert.Equal("mydb", opts.Value.DatabaseId);
    }

    /// <summary>
    /// Verifies configuration binding overload binds BrookStorageOptions from IConfiguration.
    /// </summary>
    [Fact]
    public void AddCosmosBrookStorageProviderWithConfigurationBindsOptions()
    {
        var inMemory = new Dictionary<string, string?>
        {
            ["DatabaseId"] = "confdb",
            ["LockContainerName"] = "confblobs",
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

        var services = new ServiceCollection();

        // Use the IConfiguration-only overload so we don't register SDK clients here.
        services.AddCosmosBrookStorageProvider(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<Mississippi.EventSourcing.Cosmos.BrookStorageOptions>>();

        Assert.Equal("confdb", opts.Value.DatabaseId);
        Assert.Equal("confblobs", opts.Value.LockContainerName);
    }
}