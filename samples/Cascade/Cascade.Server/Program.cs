// <copyright file="Program.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Cascade.Domain;
using Cascade.Server.Components;
using Cascade.Server.Components.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Snapshots.Cosmos;
using Mississippi.EventSourcing.UxProjections.SignalR;

using Orleans.Configuration;
using Orleans.Hosting;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire-managed clients - these register CosmosClient and BlobServiceClient
// using connection strings from Aspire AppHost references
builder.AddAzureCosmosClient("cosmos");
builder.AddAzureBlobServiceClient("blobs");

// Add Blazor services with Interactive Server rendering
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Add SignalR for real-time projections
builder.Services.AddSignalR();
builder.Services.AddUxProjectionSignalR();

// Add domain services (aggregates, handlers, reducers, projections)
builder.Services.AddCascadeDomain();

// Add Cascade.Server services (projection subscribers, user session)
builder.Services.AddCascadeServerServices();

// Add event sourcing services to DI container
builder.Services.AddEventSourcingByService();

// Configure Orleans silo
builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering()
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "cascade-dev";
            options.ServiceId = "cascade";
        })
        .AddMemoryGrainStorage("PubSubStore")
        .AddEventSourcing();
});

// Add Cosmos storage providers for event sourcing
// These use the CosmosClient and BlobServiceClient registered by Aspire
// Note: BrookStorageOptions.ContainerId is read-only with default "brooks"
builder.Services.AddCosmosBrookStorageProvider(options => { options.DatabaseId = "cascade-db"; });
builder.Services.AddCosmosSnapshotStorageProvider(options =>
{
    options.DatabaseId = "cascade-db";
    options.ContainerId = "snapshots";
});
WebApplication app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// Map SignalR hub for real-time UX projections
app.MapUxProjectionHub();

// Map Blazor components
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
await app.RunAsync();