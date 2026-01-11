// <copyright file="Program.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage (Azurite emulator) for Blob storage tests
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();
_ = storage.AddBlobs("blobs");

// Add Cosmos DB using PREVIEW emulator (Linux-based) which has proper HTTP health check
// The preview emulator exposes an HTTP /ready endpoint that properly indicates readiness
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(emulator =>
    {
        // Enable Data Explorer for debugging at http://localhost:{port}
        emulator.WithDataExplorer();
#pragma warning disable ASPIRECERTIFICATES001
        emulator.WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001
    });
IResourceBuilder<AzureCosmosDBDatabaseResource> database = cosmos.AddCosmosDatabase("testdb");
_ = database.AddContainer("testcontainer", "/id");
await builder.Build().RunAsync();