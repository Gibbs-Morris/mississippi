using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Orleans;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage (Azurite emulator)
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();
IResourceBuilder<AzureBlobStorageResource> blobs = storage.AddBlobs("blobs");

// Add Azure Table Storage for Orleans clustering
IResourceBuilder<AzureTableStorageResource> clusteringTable = storage.AddTables("clustering");

// Add Azure Blob Storage for Orleans grain state
IResourceBuilder<AzureBlobStorageResource> grainState = storage.AddBlobs("grainstate");

// Add Cosmos DB (emulator)
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator();
_ = cosmos.AddCosmosDatabase("cascade-web-db");

// Configure Orleans with Azure Storage for clustering, grain state, and in-memory streaming
OrleansService orleans = builder.AddOrleans("default")
    .WithClustering(clusteringTable)
    .WithGrainStorage("Default", grainState)
    .WithMemoryGrainStorage("PubSubStore")
    .WithMemoryStreaming("StreamProvider");

// Add Cascade.Web.Silo - Orleans server that hosts grains
// WithReference cosmos/blobs for event sourcing storage (Brooks + Snapshots)
builder.AddProject<Cascade_Web_Silo>("cascade-web-silo")
    .WithReference(orleans)
    .WithReference(cosmos)
    .WithReference(blobs)
    .WaitFor(storage)
    .WaitFor(cosmos);

// Add Cascade.Web.Server with resource references
// This is an Orleans client that connects to the silo
// WaitFor ensures the server doesn't start until dependencies are ready
builder.AddProject<Cascade_Web_Server>("cascade-web-server")
    .WithReference(orleans.AsClient())
    .WithReference(blobs)
    .WaitFor(storage)
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithExternalHttpEndpoints();
await builder.Build().RunAsync();