#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Orleans;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

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
    .WithMemoryStreaming("mississippi-streaming");

// Add Spring.Silo - Orleans server that hosts grains
// WithReference cosmos/blobs for event sourcing storage (Brooks + Snapshots)
// WithHealthCheck ensures the silo is fully ready before dependents start
IResourceBuilder<ProjectResource> silo = builder.AddProject<Spring_Silo>("spring-silo")
    .WithReference(orleans)
    .WithReference(cosmos)
    .WithReference(blobs)
    .WaitFor(storage)
    .WaitFor(cosmos)
    .WithHttpHealthCheck("/health");

// Add Spring.Server with resource references
// This is an Orleans client that connects to the silo
// WaitFor ensures the server doesn't start until dependencies are ready
builder.AddProject<Spring_Server>("spring-server")
    .WithReference(orleans.AsClient())
    .WaitFor(storage)
    .WaitFor(silo)
    .WithExternalHttpEndpoints();
await builder.Build().RunAsync();