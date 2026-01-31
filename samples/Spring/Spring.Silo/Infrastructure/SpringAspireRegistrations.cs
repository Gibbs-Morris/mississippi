using Azure.Storage.Blobs;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Common.Abstractions;


namespace Spring.Silo.Infrastructure;

/// <summary>
///     Aspire-managed Azure resource configuration for Spring silo.
/// </summary>
internal static class SpringAspireRegistrations
{
    /// <summary>
    ///     The shared Cosmos service key used by both Brooks and Snapshots.
    /// </summary>
    internal const string SharedCosmosServiceKey = "spring-cosmos";

    /// <summary>
    ///     Adds Aspire-managed Azure resources (Table, Blob, Cosmos) with Mississippi keyed forwarding.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddSpringAspireResources(
        this IHostApplicationBuilder builder
    )
    {
        AddOrleansStorageClients(builder);
        AddCosmosClient(builder);
        AddBlobClientForLocking(builder);
        ForwardToMississippiServiceKeys(builder);
        return builder;
    }

    private static void AddBlobClientForLocking(
        IHostApplicationBuilder builder
    )
    {
        // Azure Blob Storage for distributed locking (Brooks)
        builder.AddKeyedAzureBlobServiceClient("blobs");
    }

    private static void AddCosmosClient(
        IHostApplicationBuilder builder
    )
    {
        // Cosmos client for event sourcing storage (Brooks + Snapshots)
        // Gateway mode required for Aspire Cosmos emulator compatibility
        builder.AddAzureCosmosClient(
            "cosmos",
            configureClientOptions: options =>
            {
                options.ConnectionMode = ConnectionMode.Gateway;
                options.LimitToEndpoint = true;
            });
    }

    private static void AddOrleansStorageClients(
        IHostApplicationBuilder builder
    )
    {
        // Azure Table Storage for Orleans clustering (keyed for Orleans resolution)
        builder.AddKeyedAzureTableServiceClient("clustering");

        // Azure Blob Storage for Orleans grain state (keyed for Orleans resolution)
        builder.AddKeyedAzureBlobServiceClient("grainstate");
    }

    private static void ForwardToMississippiServiceKeys(
        IHostApplicationBuilder builder
    )
    {
        // Forward blob client to Mississippi's blob locking key
        builder.Services.AddKeyedSingleton(
            MississippiDefaults.ServiceKeys.BlobLocking,
            (
                sp,
                _
            ) => sp.GetRequiredKeyedService<BlobServiceClient>("blobs"));

        // Forward Cosmos client to shared key used by Brooks and Snapshots
        builder.Services.AddKeyedSingleton(
            SharedCosmosServiceKey,
            (
                sp,
                _
            ) => sp.GetRequiredService<CosmosClient>());
    }
}