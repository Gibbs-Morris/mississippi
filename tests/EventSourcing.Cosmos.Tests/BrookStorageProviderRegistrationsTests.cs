using Azure.Storage.Blobs;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions.Storage;


namespace Mississippi.EventSourcing.Cosmos.Tests;

/// <summary>
///     Unit tests for BrookStorageProviderRegistrations extension methods.
/// </summary>
public class BrookStorageProviderRegistrationsTests
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
        List<Type> serviceTypes = services.Select(sd => sd.ServiceType).ToList();
        Assert.Contains(typeof(IBrookStorageProvider), serviceTypes);
        Assert.DoesNotContain(typeof(CosmosClient), serviceTypes);
        Assert.DoesNotContain(typeof(BlobServiceClient), serviceTypes);
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
}