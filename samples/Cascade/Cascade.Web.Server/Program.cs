using System;
using System.Net.Http;

using Cascade.Web.Contracts;
using Cascade.Web.Server.Hubs;
using Cascade.Web.Server.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using BlobItemDto = Cascade.Web.Contracts.BlobItem;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire-managed clients with emulator-compatible configuration
// Gateway mode is required for Cosmos DB emulator running in container
// See: https://github.com/dotnet/aspire/issues/5364
builder.AddAzureCosmosClient(
    "cosmos",
    configureClientOptions: options =>
    {
        options.ConnectionMode = ConnectionMode.Gateway;
        options.LimitToEndpoint = true;

        // Bypass SSL certificate validation for emulator's self-signed cert
#pragma warning disable CA5400, S4830 // HttpClient certificate check - emulator uses self-signed cert
#pragma warning disable IDISP014, IDISP001 // Use a single instance of HttpClient - CosmosClient manages its own lifecycle
        options.HttpClientFactory = () => new HttpClient(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });
#pragma warning restore IDISP014, IDISP001
#pragma warning restore CA5400, S4830
    });
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
