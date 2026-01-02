// <copyright file="Program.cs" company="Corsair Software Ltd">
// Copyright (c) Corsair Software Ltd. All rights reserved.
// Licensed under the Apache-2.0 License. See LICENSE file in the project root for full license information.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add Azure Storage (Azurite emulator)
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();
IResourceBuilder<AzureBlobStorageResource> blobs = storage.AddBlobs("blobs");

// Add Cosmos DB (emulator)
IResourceBuilder<AzureCosmosDBDatabaseResource> cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator()
    .AddCosmosDatabase("cascade-db");

// Add Cascade Server with resource references
builder.AddProject<Cascade_Server>("cascade-server")
    .WithReference(blobs)
    .WithReference(cosmos)
    .WithExternalHttpEndpoints();
await builder.Build().RunAsync();