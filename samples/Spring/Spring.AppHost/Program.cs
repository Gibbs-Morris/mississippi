using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Orleans;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage (Azurite emulator)
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();

// Add Azure Table Storage for Orleans clustering
IResourceBuilder<AzureTableStorageResource> clusteringTable = storage.AddTables("clustering");

// Add Azure Blob Storage for Orleans grain state
IResourceBuilder<AzureBlobStorageResource> grainState = storage.AddBlobs("grainstate");

// Configure Orleans with Azure Storage for clustering and grain state
OrleansService orleans = builder.AddOrleans("default")
    .WithClustering(clusteringTable)
    .WithGrainStorage("Default", grainState);

// Add Spring.Silo - Orleans server that hosts grains
// WithHealthCheck ensures the silo is fully ready before dependents start
IResourceBuilder<ProjectResource> silo = builder.AddProject<Spring_Silo>("spring-silo")
    .WithReference(orleans)
    .WaitFor(storage)
    .WithHttpHealthCheck("/health");

// Add Spring.Server with resource references
// This is an Orleans client that connects to the silo
builder.AddProject<Spring_Server>("spring-server")
    .WithReference(orleans.AsClient())
    .WaitFor(storage)
    .WaitFor(silo)
    .WithExternalHttpEndpoints();
await builder.Build().RunAsync();