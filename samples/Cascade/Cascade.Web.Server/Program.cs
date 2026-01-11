using System;

using Cascade.Web.Contracts;
using Cascade.Web.Server.Hubs;
using Cascade.Web.Server.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using BlobItemDto = Cascade.Web.Contracts.BlobItem;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire-managed clients
builder.AddAzureCosmosClient("cosmos");
builder.AddAzureBlobServiceClient("blobs");

// Add SignalR for real-time messaging
builder.Services.AddSignalR();

// Add storage services
builder.Services.AddSingleton<ICosmosService, CosmosService>();
builder.Services.AddSingleton<IBlobService, BlobService>();

WebApplication app = builder.Build();

// Serve Blazor WebAssembly static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

// Map SignalR hub
app.MapHub<MessageHub>("/hubs/messages");

// Map API endpoints
app.MapGet(
    "/api/health",
    () => new HealthResponse { Status = "Healthy", Timestamp = DateTime.UtcNow });

app.MapGet(
    "/api/echo",
    (string message) => new EchoResponse { Message = message, ReceivedAt = DateTime.UtcNow });

app.MapGet(
    "/api/cosmos",
    async (ICosmosService cosmosService) => await cosmosService.GetItemsAsync());

app.MapPost(
    "/api/cosmos",
    async (CosmosItem item, ICosmosService cosmosService) =>
    {
        await cosmosService.CreateItemAsync(item);
        return Results.Created($"/api/cosmos/{item.Id}", item);
    });

app.MapGet(
    "/api/blob/{name}",
    async (string name, IBlobService blobService) =>
    {
        BlobItemDto? item = await blobService.GetBlobAsync(name);
        return item is not null ? Results.Ok(item) : Results.NotFound();
    });

app.MapPost(
    "/api/blob",
    async (BlobItemDto item, IBlobService blobService) =>
    {
        await blobService.UploadBlobAsync(item);
        return Results.Created($"/api/blob/{item.Name}", item);
    });

// Fallback to index.html for client-side routing
app.MapFallbackToFile("index.html");

await app.RunAsync();
