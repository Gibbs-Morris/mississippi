using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();
_ = storage.AddBlobs("blobs");
await builder.Build().RunAsync();
