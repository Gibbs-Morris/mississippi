using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage (Azurite emulator)
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();
IResourceBuilder<AzureBlobStorageResource> blobs = storage.AddBlobs("blobs");

// Add Cosmos DB (emulator)
// The cosmos resource publishes the connection string under name "cosmos"
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator();
_ = cosmos.AddCosmosDatabase("cascade-db");

// Add Cascade Server with resource references
// WaitFor ensures the server doesn't start until emulators are ready
builder.AddProject<Cascade_Server>("cascade-server")
    .WithReference(blobs)
    .WaitFor(storage)
    .WithReference(cosmos) // Reference the parent cosmos resource for connection string
    .WaitFor(cosmos)
    .WithExternalHttpEndpoints();
await builder.Build().RunAsync();