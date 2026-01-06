using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage (Azurite emulator)
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();
IResourceBuilder<AzureBlobStorageResource> blobs = storage.AddBlobs("blobs");
IResourceBuilder<AzureTableStorageResource> clusteringTable = storage.AddTables("clustering");

// Add Cosmos DB (emulator)
// The cosmos resource publishes the connection string under name "cosmos"
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator();
_ = cosmos.AddCosmosDatabase("cascade-db");

// Add Orleans resource with Azure Table Storage clustering
IResourceBuilder<OrleansService> orleans = builder.AddOrleans("default")
    .WithClustering(clusteringTable);

// Add Cascade Server (Orleans silo) with resource references
// WaitFor ensures the server doesn't start until emulators are ready
builder.AddProject<Cascade_Server>("cascade-server")
    .WithReference(blobs)
    .WaitFor(storage)
    .WithReference(cosmos) // Reference the parent cosmos resource for connection string
    .WaitFor(cosmos)
    .WithReference(orleans) // Reference Orleans as a silo
    .WithExternalHttpEndpoints();

// Add Cascade WebApi (Orleans client) serving WASM with resource references
builder.AddProject<Cascade_WebApi>("cascade-webapi")
    .WithReference(blobs)
    .WaitFor(storage)
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithReference(orleans.AsClient()) // Reference Orleans as a client
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();