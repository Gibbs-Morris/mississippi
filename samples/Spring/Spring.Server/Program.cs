using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Aqueduct.Gateway;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Runtime;
using Mississippi.Inlet.Gateway;
using Mississippi.Inlet.Runtime;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans.Hosting;

using Scalar.AspNetCore;

using Spring.Domain.Projections.BankAccountBalance;
using Spring.Server.Controllers.Mappers;
using Spring.Server.McpTools;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Microsoft.Orleans.Runtime")
        .AddSource("Microsoft.Orleans.Application"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Microsoft.Orleans"))
    .WithLogging()
    .UseOtlpExporter();

// Add Aspire-managed Azure Table client for Orleans clustering
// Must use Keyed variant so Orleans can resolve via GetRequiredKeyedService<T>(serviceKey)
builder.AddKeyedAzureTableServiceClient("clustering");

// Configure Orleans client - Aspire injects clustering config via environment variables
builder.UseOrleansClient(clientBuilder => { clientBuilder.AddActivityPropagation(); });

// Add controllers for aggregate API endpoints
builder.Services.AddControllers();

// Add OpenAPI documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((
        document,
        _,
        _
    ) =>
    {
        document.Info.Title = "Spring Bank API";
        document.Info.Version = "v1";
        document.Info.Description = "Event-sourced banking API built with Mississippi framework";
        return Task.CompletedTask;
    });
});

// Add JSON serialization provider (required by aggregate infrastructure)
builder.Services.AddJsonSerialization();

// Add aggregate infrastructure support (IAggregateGrainFactory, IBrookEventConverter, etc.)
builder.Services.AddAggregateSupport();

// Add UX projection infrastructure support (IUxProjectionGrainFactory)
builder.Services.AddUxProjections();

// Add Aqueduct backplane for InletHub (registers IServerIdProvider and other dependencies)
builder.Services.AddSignalR();
builder.Services.AddAqueduct<InletHub>(options =>
{
    // Use the same stream provider configured by Aspire's WithMemoryStreaming
    options.StreamProviderName = "StreamProvider";
});

// Add Inlet Server services for real-time projection updates
builder.Services.AddInletServer();
builder.Services.ScanProjectionAssemblies(typeof(BankAccountBalanceProjection).Assembly);

// Add generated domain mapper registrations
builder.Services.AddSpringDomainServer();

// Add MCP (Model Context Protocol) server with HTTP transport
// Exposes banking domain operations as tools for AI agents via source-generated tool classes.
builder.Services.AddMcpServer().WithHttpTransport().WithGeneratedMcpTools().WithTools<SpringServerPingMcpTools>();
WebApplication app = builder.Build();

// Serve Blazor WebAssembly static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

// OpenAPI documentation endpoints
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Spring Bank API");
    options.WithTheme(ScalarTheme.BluePlanet);
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// Map controllers before API endpoints
app.MapControllers();

// Map MCP endpoint for AI agent tool invocation (development only)
if (app.Environment.IsDevelopment())
{
    app.MapMcp("/mcp");
}

// Map Inlet hub for real-time projection updates
app.MapInletHub();

// Health check endpoint for Aspire resource health monitoring
app.MapGet(
    "/health",
    () => Results.Ok(
        new
        {
            status = "healthy",
        }));

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");
await app.RunAsync();