using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage (Azurite emulator)
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();
IResourceBuilder<AzureBlobStorageResource> blobs = storage.AddBlobs("blobs");

// Add Cosmos DB (emulator)
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator();
_ = cosmos.AddCosmosDatabase("cascade-web-db");

// Add Cascade.Web.Server with resource references
// WaitFor ensures the server doesn't start until emulators are ready
builder.AddProject<Cascade_Web_Server>("cascade-web-server")
    .WithReference(blobs)
    .WaitFor(storage)
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
