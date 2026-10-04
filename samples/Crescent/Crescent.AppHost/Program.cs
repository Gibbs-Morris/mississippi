// <copyright file="Program.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage (Azurite emulator) for Blob storage tests
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();
_ = storage.AddBlobs("blobs");

// Add Cosmos DB using the Linux vNext emulator with its HTTP /ready health check
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator(emulator =>
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