using System;
using System.Linq;
using System.Net.Http;

using Azure.Storage.Blobs;

using Cascade.Domain.Conversation;
using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.Projections.ChannelMessages;
using Cascade.Web.Contracts;
using Cascade.Web.Contracts.Grains;
using Cascade.Web.Server.Hubs;
using Cascade.Web.Server.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Aqueduct;
using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.UxProjections;
using Mississippi.EventSourcing.UxProjections.Abstractions;

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
        options.HttpClientFactory = () => new(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });
#pragma warning restore IDISP014, IDISP001
#pragma warning restore CA5400, S4830
    });

// Add Aspire-managed Blob client for storage operations
// Register keyed first, then forward to unkeyed for BlobService
builder.AddKeyedAzureBlobServiceClient("blobs");
builder.Services.AddSingleton(sp => sp.GetRequiredKeyedService<BlobServiceClient>("blobs"));

// Add Aspire-managed Azure Table client for Orleans clustering
// Must use Keyed variant so Orleans can resolve via GetRequiredKeyedService<T>(serviceKey)
builder.AddKeyedAzureTableServiceClient("clustering");

// Configure Orleans client to connect to the silo cluster
// UseOrleansClient() automatically configures clustering based on the Azure Table client
builder.UseOrleansClient();

// Add SignalR for real-time messaging with Orleans backplane
builder.Services.AddSignalR();
builder.Services.AddAqueduct<MessageHub>(options =>
{
    // Use the same stream provider configured by Aspire's WithMemoryStreaming
    options.StreamProviderName = "StreamProvider";
});

// Add storage services
builder.Services.AddSingleton<ICosmosService, CosmosService>();
builder.Services.AddSingleton<IBlobService, BlobService>();

// TEMPORARY PLUMBING - TO BE REPLACED BY INLET
// These registrations enable direct grain access from the BFF server.
// Once Inlet is integrated, the client will communicate via Inlet's
// built-in SignalR hub and these registrations may move or be replaced.
builder.Services.AddJsonSerialization();
builder.Services.AddAggregateSupport();
builder.Services.AddUxProjections();
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
    () => new HealthResponse
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
    });
app.MapGet(
    "/api/echo",
    (
        string message
    ) => new EchoResponse
    {
        Message = message,
        ReceivedAt = DateTime.UtcNow,
    });

// Orleans grain endpoint - demonstrates WebAssembly → API → Orleans grain flow
app.MapGet(
    "/api/greet/{name}",
    async (
        string name,
        IGrainFactory grainFactory
    ) =>
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
    async (
        ToUpperRequest request,
        IGrainFactory grainFactory
    ) =>
    {
        IGreeterGrain grain = grainFactory.GetGrain<IGreeterGrain>("converter");
        string result = await grain.ToUpperAsync(request.Input);
        return Results.Text(result);
    });

// Orleans streaming endpoint - broadcasts a message via Orleans streams
// The message flows: API → BroadcasterGrain → Orleans Stream → StreamBridgeService → SignalR → Clients
app.MapPost(
    "/api/broadcast",
    async (
        BroadcastRequest request,
        IGrainFactory grainFactory
    ) =>
    {
        IBroadcasterGrain grain = grainFactory.GetGrain<IBroadcasterGrain>("default");
        await grain.BroadcastAsync(request.Message);
        return Results.Ok(
            new
            {
                request.Message,
                Status = "Broadcast sent",
            });
    });

// Products endpoint for Reservoir effect demo - returns a static list of products
app.MapGet("/api/products", () => AvailableProducts);
app.MapGet(
    "/api/cosmos",
    async (
        ICosmosService cosmosService
    ) => await cosmosService.GetItemsAsync());
app.MapPost(
    "/api/cosmos",
    async (
        CosmosItem item,
        ICosmosService cosmosService
    ) =>
    {
        await cosmosService.CreateItemAsync(item);
        return Results.Created($"/api/cosmos/{item.Id}", item);
    });
app.MapGet(
    "/api/blob/{name}",
    async (
        string name,
        IBlobService blobService
    ) =>
    {
        BlobItemDto? item = await blobService.GetBlobAsync(name);
        return item is not null ? Results.Ok(item) : Results.NotFound();
    });
app.MapPost(
    "/api/blob",
    async (
        BlobItemDto item,
        IBlobService blobService
    ) =>
    {
        await blobService.UploadBlobAsync(item);
        return Results.Created($"/api/blob/{item.Name}", item);
    });

// ┌────────────────────────────────────────────────────────────────────────────────┐
// │ TEMPORARY PLUMBING - TO BE REPLACED BY INLET                                   │
// │                                                                                 │
// │ These endpoints manually bridge the Blazor client to Orleans aggregate/        │
// │ projection grains. Once Inlet is integrated, it will handle:                   │
// │   - Automatic projection subscriptions via SignalR                             │
// │   - Command dispatch without explicit HTTP endpoints                           │
// │   - Real-time projection updates pushed to clients                             │
// │                                                                                 │
// │ Search for "TEMPORARY PLUMBING - TO BE REPLACED BY INLET" to find all          │
// │ locations that need to be updated when Inlet is integrated.                    │
// └────────────────────────────────────────────────────────────────────────────────┘

// Start a conversation (idempotent)
app.MapPost(
    "/api/conversations/{conversationId}/start",
    async (
        string conversationId,
        IAggregateGrainFactory aggregateGrainFactory
    ) =>
    {
        IGenericAggregateGrain<ConversationAggregate> grain =
            aggregateGrainFactory.GetGenericAggregate<ConversationAggregate>(conversationId);
        OperationResult result = await grain.ExecuteAsync(
            new StartConversation
            {
                ConversationId = conversationId,
                ChannelId = conversationId, // Use same ID for simplicity
            });
        return result.Success
            ? Results.Ok(
                new
                {
                    ConversationId = conversationId,
                    Status = "Started",
                })
            : Results.BadRequest(
                new
                {
                    result.ErrorCode,
                    result.ErrorMessage,
                });
    });

// Send a message to a conversation
app.MapPost(
    "/api/conversations/{conversationId}/messages",
    async (
        string conversationId,
        SendMessageRequest request,
        IAggregateGrainFactory aggregateGrainFactory
    ) =>
    {
        IGenericAggregateGrain<ConversationAggregate> grain =
            aggregateGrainFactory.GetGenericAggregate<ConversationAggregate>(conversationId);
        string messageId = $"msg-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..32];
        OperationResult result = await grain.ExecuteAsync(
            new SendMessage
            {
                MessageId = messageId,
                Content = request.Content,
                SentBy = request.SentBy,
            });
        return result.Success
            ? Results.Ok(
                new
                {
                    MessageId = messageId,
                    Status = "Sent",
                })
            : Results.BadRequest(
                new
                {
                    result.ErrorCode,
                    result.ErrorMessage,
                });
    });

// Get messages projection for a conversation
app.MapGet(
    "/api/conversations/{conversationId}/messages",
    async (
        string conversationId,
        IUxProjectionGrainFactory uxProjectionGrainFactory
    ) =>
    {
        IUxProjectionGrain<ChannelMessagesProjection> grain =
            uxProjectionGrainFactory.GetUxProjectionGrain<ChannelMessagesProjection>(conversationId);
        ChannelMessagesProjection? projection = await grain.GetAsync();
        if (projection is null)
        {
            return Results.NotFound();
        }

        // Map internal projection to public DTO
        ConversationMessagesResponse response = new()
        {
            ConversationId = conversationId,
            MessageCount = projection.MessageCount,
            Messages = projection.Messages.Select(m => new ConversationMessageItem
                {
                    MessageId = m.MessageId,
                    Content = m.Content,
                    SentBy = m.SentBy,
                    SentAt = m.SentAt,
                    EditedAt = m.EditedAt,
                    IsDeleted = m.IsDeleted,
                })
                .ToList(),
        };
        return Results.Ok(response);
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