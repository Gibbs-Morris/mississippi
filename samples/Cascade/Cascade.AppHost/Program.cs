#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
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

// Add Cosmos DB using PREVIEW emulator (Linux-based) which has proper HTTP health check
// The preview emulator exposes an HTTP /ready endpoint that properly indicates readiness
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(emulator =>
    {
        // Enable Data Explorer for debugging at http://localhost:1234
        emulator.WithDataExplorer();
#pragma warning disable ASPIRECERTIFICATES001
        emulator.WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001
    });
_ = cosmos.AddCosmosDatabase("cascade-web-db");

// Configure Orleans with Azure Storage for clustering, grain state, and in-memory streaming
OrleansService orleans = builder.AddOrleans("default")
    .WithClustering(clusteringTable)
    .WithGrainStorage("Default", grainState)
    .WithMemoryGrainStorage("PubSubStore")
    .WithMemoryStreaming("StreamProvider");

// Add Cascade.Silo - Orleans server that hosts grains
// WithReference cosmos/blobs for event sourcing storage (Brooks + Snapshots)
// WithHealthCheck ensures the silo is fully ready (Orleans registered) before dependents start
IResourceBuilder<ProjectResource> silo = builder.AddProject<Cascade_Silo>("cascade-silo")
    .WithReference(orleans)
    .WithReference(cosmos)
    .WithReference(blobs)
    .WaitFor(storage)
    .WaitFor(cosmos)
    .WithHttpHealthCheck("/health");

// Add Cascade.Server with resource references
// This is an Orleans client that connects to the silo
// WaitFor ensures the server doesn't start until dependencies are ready
builder.AddProject<Cascade_Server>("cascade-server")
    .WithReference(orleans.AsClient())
    .WithReference(blobs)
    .WaitFor(storage)
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WaitFor(silo)
    .WithExternalHttpEndpoints();
await builder.Build().RunAsync();