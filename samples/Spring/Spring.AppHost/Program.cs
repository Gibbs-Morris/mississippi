#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Orleans;

using Microsoft.Extensions.Configuration;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
bool springAuthProofModeEnabled = builder.Configuration.GetValue("Spring:AuthProofMode", false);

// Add Azure Storage (Azurite emulator)
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();

// Add Azure Blob Storage for distributed locking (Brooks)
IResourceBuilder<AzureBlobStorageResource> blobs = storage.AddBlobs("blobs");

// Add Azure Table Storage for Orleans clustering
IResourceBuilder<AzureTableStorageResource> clusteringTable = storage.AddTables("clustering");

// Add Azure Blob Storage for Orleans grain state
IResourceBuilder<AzureBlobStorageResource> grainState = storage.AddBlobs("grainstate");

// Add Cosmos DB using PREVIEW emulator for event sourcing storage (Brooks + Snapshots)
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithDataExplorer();
#pragma warning disable ASPIRECERTIFICATES001
        emulator.WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001
    });
_ = cosmos.AddCosmosDatabase("spring-db");

// Configure Orleans with Azure Storage for clustering, grain state, and in-memory streaming
OrleansService orleans = builder.AddOrleans("default")
    .WithClustering(clusteringTable)
    .WithGrainStorage("Default", grainState)
    .WithMemoryGrainStorage("PubSubStore")
    .WithMemoryStreaming("StreamProvider");

// Add Spring.Runtime - business runtime host (runs in an Orleans silo)
// WithReference cosmos/blobs for event sourcing storage (Brooks + Snapshots)
// WithHealthCheck ensures the runtime host is fully ready before dependents start
IResourceBuilder<ProjectResource> springRuntime = builder.AddProject<Spring_Runtime>("spring-runtime")
    .WithReference(orleans)
    .WithReference(cosmos)
    .WithReference(blobs)
    .WaitFor(storage)
    .WaitFor(cosmos)
    .WithHttpHealthCheck("/health");

// Add Spring.Gateway with resource references
// This host is an Orleans client that connects to the runtime host's Orleans silo
// WaitFor ensures the gateway doesn't start until dependencies are ready
IResourceBuilder<ProjectResource> springGateway = builder.AddProject<Spring_Gateway>("spring-gateway")
    .WithReference(orleans.AsClient())
    .WaitFor(storage)
    .WaitFor(springRuntime)
    .WithExternalHttpEndpoints();
if (springAuthProofModeEnabled)
{
    springGateway.WithEnvironment("SpringAuth__Enabled", "true");
}

await builder.Build().RunAsync();