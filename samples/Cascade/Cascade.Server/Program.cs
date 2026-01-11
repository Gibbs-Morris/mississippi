using Cascade.Domain;
using Cascade.Server.Components;
using Cascade.Server.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.Snapshots.Cosmos;
using Mississippi.Inlet.Orleans.SignalR;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans.Hosting;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry for metrics and tracing
// Aspire sets OTEL_EXPORTER_OTLP_ENDPOINT for automatic dashboard integration
// Note: In OpenTelemetry 1.14.0+, exporters are added inside WithMetrics/WithTracing
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        // Metrics provider from OpenTelemetry
        metrics.AddAspNetCoreInstrumentation();

        // Metrics provides by ASP.NET Core in .NET 9
        metrics.AddMeter("Microsoft.AspNetCore.Hosting");
        metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");

        // Export via OTLP when endpoint is configured (set by Aspire)
        if (otlpEndpoint != null)
        {
            metrics.AddOtlpExporter();
        }
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddSource("Microsoft.Orleans.Runtime");
        tracing.AddSource("Microsoft.Orleans.Application");

        // Export via OTLP when endpoint is configured (set by Aspire)
        if (otlpEndpoint != null)
        {
            tracing.AddOtlpExporter();
        }
    });

// Add Aspire-managed clients - these register CosmosClient and BlobServiceClient
// using connection strings from Aspire AppHost references
builder.AddAzureCosmosClient("cosmos");
builder.AddAzureBlobServiceClient("blobs");

// Add Blazor services with Interactive Server rendering
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Add SignalR with Orleans backplane for real-time projections
builder.Services.AddInletOrleansWithSignalR();

// Add domain services (aggregates, handlers, reducers, projections)
builder.Services.AddCascadeDomain();

// Add Cascade.Server services (projection subscribers, user session)
builder.Services.AddCascadeServerServices();

// Add event sourcing services to DI container
builder.Services.AddJsonSerialization();
builder.Services.AddEventSourcingByService();

// Configure Orleans silo
// Use localhost clustering for local development (single-node cluster)
// For production, configure Azure Table Storage clustering
builder.UseOrleans(silo =>
{
    // Use localhost clustering for local development
    silo.UseLocalhostClustering();

    // Add memory storage for PubSub (required for Orleans Streams)
    silo.AddMemoryGrainStorage("PubSubStore");

    // Add Mississippi event sourcing streams (SMS provider for real-time projections)
    silo.AddEventSourcing();
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
app.MapInletHub();

// Map Blazor components
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
await app.RunAsync();