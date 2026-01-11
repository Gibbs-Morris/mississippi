using System;
using System.Net.Http;

using Cascade.Web.Contracts;
using Cascade.Web.Contracts.Grains;
using Cascade.Web.Server.Hubs;
using Cascade.Web.Server.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Orleans;

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

// Add Aspire-managed Azure Table client for Orleans clustering
// Must use Keyed variant so Orleans can resolve via GetRequiredKeyedService<T>(serviceKey)
builder.AddKeyedAzureTableServiceClient("clustering");

// Configure Orleans client to connect to the silo cluster
// UseOrleansClient() automatically configures clustering based on the Azure Table client
builder.UseOrleansClient();

// Add SignalR for real-time messaging
builder.Services.AddSignalR();

// Add storage services
builder.Services.AddSingleton<ICosmosService, CosmosService>();
builder.Services.AddSingleton<IBlobService, BlobService>();

// Add Orleans stream to SignalR bridge service
builder.Services.AddHostedService<StreamBridgeService>();

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

// Orleans grain endpoint - demonstrates WebAssembly → API → Orleans grain flow
app.MapGet(
    "/api/greet/{name}",
    async (string name, IGrainFactory grainFactory) =>
    {
        IGreeterGrain grain = grainFactory.GetGrain<IGreeterGrain>(name);
        GreetResponse response = await grain.GreetAsync();

        // Map to API-friendly response (without Orleans serialization attributes)
        return Results.Ok(
            new GreetApiResponse
            {
                Greeting = response.Greeting,
                UppercaseName = response.UppercaseName,
                Timestamp = response.GeneratedAt,
            });
    });

// Orleans grain endpoint - uppercase conversion
app.MapPost(
    "/api/toupper",
    async (ToUpperRequest request, IGrainFactory grainFactory) =>
    {
        IGreeterGrain grain = grainFactory.GetGrain<IGreeterGrain>("converter");
        string result = await grain.ToUpperAsync(request.Input);
        return Results.Text(result);
    });

// Orleans streaming endpoint - broadcasts a message via Orleans streams
// The message flows: API → BroadcasterGrain → Orleans Stream → StreamBridgeService → SignalR → Clients
app.MapPost(
    "/api/broadcast",
    async (BroadcastRequest request, IGrainFactory grainFactory) =>
    {
        IBroadcasterGrain grain = grainFactory.GetGrain<IBroadcasterGrain>("default");
        await grain.BroadcastAsync(request.Message);
        return Results.Ok(new { Message = request.Message, Status = "Broadcast sent" });
    });

// Products endpoint for Reservoir effect demo - returns a static list of products
app.MapGet("/api/products", () => AvailableProducts);

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

/// <summary>
///     Placeholder class for top-level partial declarations.
/// </summary>
internal static partial class Program
{
    /// <summary>
    ///     Static list of available products for the Reservoir effect demo.
    /// </summary>
    private static readonly string[] AvailableProducts =
    [
        "Apple",
        "Banana",
        "Orange",
        "Milk",
        "Bread",
        "Cheese",
        "Eggs",
        "Butter",
    ];
}
