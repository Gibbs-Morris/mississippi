using System;
using System.Net.Http;

using Azure.Storage.Blobs;

using Cascade.Contracts.Api;
using Cascade.Contracts.Storage;
using Cascade.Domain.Aggregates.Channel;
using Cascade.Domain.Aggregates.Channel.Commands;
using Cascade.Domain.Conversation;
using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.Projections.ChannelMessages;
using Cascade.Domain.User;
using Cascade.Domain.User.Commands;
using Cascade.Grains.Abstractions;
using Cascade.Server.Hubs;
using Cascade.Server.Services;

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
using Mississippi.EventSourcing.UxProjections.Api;
using Mississippi.Inlet.Orleans;
using Mississippi.Inlet.Orleans.SignalR;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans;
using Orleans.Hosting;

using BlobItemDto = Cascade.Contracts.Storage.BlobItem;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Microsoft.Orleans.Runtime")
        .AddSource("Microsoft.Orleans.Application"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()

        // Mississippi framework meters
        .AddMeter("Mississippi.EventSourcing.Brooks")
        .AddMeter("Mississippi.EventSourcing.Aggregates")
        .AddMeter("Mississippi.EventSourcing.Snapshots")
        .AddMeter("Mississippi.EventSourcing.UxProjections")
        .AddMeter("Mississippi.Aqueduct")
        .AddMeter("Mississippi.Inlet")
        .AddMeter("Mississippi.Storage.Cosmos")
        .AddMeter("Mississippi.Storage.Snapshots")

        // Orleans meters
        .AddMeter("Microsoft.Orleans"))
    .WithLogging()
    .UseOtlpExporter();

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
// UseOrleansClient() reads Aspire-injected config and calls UseAzureStorageClustering with the keyed TableServiceClient
builder.UseOrleansClient(clientBuilder =>
{
    // Enable distributed tracing context propagation across grain calls
    clientBuilder.AddActivityPropagation();
});

// Add SignalR for real-time messaging with Orleans backplane
builder.Services.AddSignalR();
builder.Services.AddAqueduct<MessageHub>(options =>
{
    // Use the same stream provider configured by Aspire's WithMemoryStreaming
    options.StreamProviderName = "StreamProvider";
});

// Add Inlet Orleans SignalR services for real-time projection updates
// This registers the projection brook registry and configures the Inlet hub
builder.Services.AddInletOrleansWithSignalR();
builder.Services.ScanProjectionAssemblies(typeof(ChannelMessagesProjection).Assembly);

// Add storage services
builder.Services.AddSingleton<ICosmosService, CosmosService>();
builder.Services.AddSingleton<IBlobService, BlobService>();

// Event sourcing support for aggregate and projection grain access
// These registrations enable grain access from the BFF server for HTTP endpoints.
// The Inlet hub provides real-time projection updates; HTTP endpoints serve as
// fallback for data fetching and command dispatch.
builder.Services.AddJsonSerialization();
builder.Services.AddAggregateSupport();
builder.Services.AddUxProjections();
WebApplication app = builder.Build();

// Serve Blazor WebAssembly static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

// Map SignalR hubs
app.MapHub<MessageHub>("/hubs/messages");
app.MapInletHub(); // Maps to /hubs/inlet for real-time projection updates

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
        GreetResult response = await grain.GreetAsync();

        // Map to API-friendly response (without Orleans serialization attributes)
        return Results.Ok(
            new GreetingResponse
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
app.MapGet("/api/products", () => Cascade.Server.Program.AvailableProducts);

// Note: Channel discovery now uses the AllChannelIds projection via Inlet.
// Clients subscribe to "cascade/channel-ids" for real-time channel list updates.
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
// │ COMMAND ENDPOINTS                                                               │
// │                                                                                 │
// │ These endpoints dispatch commands to Orleans aggregate grains.                  │
// │                                                                                 │
// │ ✅ Projection subscriptions and real-time updates are handled by Inlet.         │
// │ ✅ Generated projection controllers provide HTTP fallback for projection data.   │
// │                                                                                 │
// │ Command dispatch via HTTP is the current pattern. Future Inlet versions may    │
// │ add SignalR-based command dispatch for reduced latency.                         │
// └────────────────────────────────────────────────────────────────────────────────┘

// Register a new user (idempotent)
app.MapPost(
    "/api/users/{userId}/register",
    async (
        string userId,
        string displayName,
        IAggregateGrainFactory aggregateGrainFactory
    ) =>
    {
        IGenericAggregateGrain<UserAggregate> grain = aggregateGrainFactory.GetGenericAggregate<UserAggregate>(userId);
        OperationResult result = await grain.ExecuteAsync(
            new RegisterUser
            {
                UserId = userId,
                DisplayName = displayName,
            });
        return result.Success
            ? Results.Ok(
                new
                {
                    UserId = userId,
                    DisplayName = displayName,
                    Status = "Registered",
                })
            : Results.BadRequest(
                new
                {
                    result.ErrorCode,
                    result.ErrorMessage,
                });
    });

// Create a new channel (idempotent)
// Note: The ChannelCreated event flows through Brooks to update AllChannelIds
// and ChannelSummary projections automatically.
app.MapPost(
    "/api/channels/{channelId}/create",
    async (
        string channelId,
        string name,
        string createdBy,
        IAggregateGrainFactory aggregateGrainFactory
    ) =>
    {
        IGenericAggregateGrain<ChannelAggregate> grain =
            aggregateGrainFactory.GetGenericAggregate<ChannelAggregate>(channelId);
        OperationResult result = await grain.ExecuteAsync(
            new CreateChannel
            {
                ChannelId = channelId,
                Name = name,
                CreatedBy = createdBy,
            });
        return result.Success
            ? Results.Ok(
                new
                {
                    ChannelId = channelId,
                    Name = name,
                    Status = "Created",
                })
            : Results.BadRequest(
                new
                {
                    result.ErrorCode,
                    result.ErrorMessage,
                });
    });

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

// Join a channel - adds user to channel and channel to user's list
app.MapPost(
    "/api/users/{userId}/channels/{channelId}/join",
    async (
        string userId,
        string channelId,
        string? channelName,
        IAggregateGrainFactory aggregateGrainFactory
    ) =>
    {
        // Add user to channel's member list
        IGenericAggregateGrain<ChannelAggregate> channelGrain =
            aggregateGrainFactory.GetGenericAggregate<ChannelAggregate>(channelId);
        OperationResult channelResult = await channelGrain.ExecuteAsync(
            new AddMember
            {
                UserId = userId,
            });

        // Add channel to user's channel list
        IGenericAggregateGrain<UserAggregate> userGrain =
            aggregateGrainFactory.GetGenericAggregate<UserAggregate>(userId);
        OperationResult userResult = await userGrain.ExecuteAsync(
            new JoinChannel
            {
                ChannelId = channelId,
                ChannelName = channelName ?? channelId,
            });

        // Both operations should succeed (or be idempotent)
        return (channelResult.Success || (channelResult.ErrorCode == "AlreadyMember")) &&
               (userResult.Success || (userResult.ErrorCode == "AlreadyJoined"))
            ? Results.Ok(
                new
                {
                    UserId = userId,
                    ChannelId = channelId,
                    Status = "Joined",
                })
            : Results.BadRequest(
                new
                {
                    ChannelError = channelResult.ErrorMessage,
                    UserError = userResult.ErrorMessage,
                });
    });

// NOTE: Channel messages projection is served via generated projection controllers at:
// GET /api/projections/cascade/channels/{entityId}
// The endpoint includes ETag support for caching

// Fallback to index.html for client-side routing
app.MapFallbackToFile("index.html");
await app.RunAsync();

namespace Cascade.Server
{
    /// <summary>
    ///     Placeholder class for top-level partial declarations.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        ///     Static list of available products for the Reservoir effect demo.
        /// </summary>
        internal static readonly string[] AvailableProducts =
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
}