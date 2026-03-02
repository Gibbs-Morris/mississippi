using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Aqueduct.Gateway;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.Common.Builders.Core;
using Mississippi.Common.Builders.Gateway;
using Mississippi.DomainModeling.Runtime;
using Mississippi.Inlet.Gateway;
using Mississippi.Inlet.Runtime;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans.Hosting;

using Scalar.AspNetCore;

using Spring.Domain.Projections.BankAccountBalance;
using Spring.Gateway;
using Spring.Gateway.Controllers.Mappers;
using Spring.Gateway.McpTools;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
GatewayBuilder gateway = GatewayBuilder.Create();
SpringAuthOptions springAuthOptions = builder.Configuration.GetSection("SpringAuth").Get<SpringAuthOptions>() ?? new();
gateway.Services.Configure<SpringAuthOptions>(builder.Configuration.GetSection("SpringAuth"));
if (springAuthOptions.Enabled && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Spring local development authentication can only be enabled in Development.");
}

gateway.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = springAuthOptions.Scheme;
        options.DefaultChallengeScheme = springAuthOptions.Scheme;
    })
    .AddScheme<AuthenticationSchemeOptions, SpringLocalDevAuthenticationHandler>(springAuthOptions.Scheme, _ => { });
gateway.Services.AddAuthorizationBuilder()
    .AddPolicy("spring.generated-api", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("spring.write", policy => policy.RequireRole("banking-operator"))
    .AddPolicy("spring.transfer", policy => policy.RequireRole("transfer-operator", "banking-operator"))
    .AddPolicy("spring.auth-proof.claim", policy => policy.RequireClaim("spring.permission", "auth-proof"));
gateway.Services.AddOpenTelemetry()
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
gateway.Services.AddControllers();

// Add OpenAPI documentation
gateway.Services.AddOpenApi(options =>
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
gateway.Services.AddJsonSerialization();

// Add aggregate infrastructure support (IAggregateGrainFactory, IBrookEventConverter, etc.)
gateway.Services.AddAggregateSupport();

// Add UX projection infrastructure support (IUxProjectionGrainFactory)
gateway.Services.AddUxProjections();

// Add Aqueduct backplane for InletHub (registers IServerIdProvider and other dependencies)
gateway.Services.AddSignalR();
gateway.Services.AddAqueduct<InletHub>(options =>
{
    // Use the same stream provider configured by Aspire's WithMemoryStreaming
    options.StreamProviderName = "StreamProvider";
});

// Add Inlet Gateway services for real-time projection updates
if (springAuthOptions.Enabled)
{
    gateway.ConfigureAuthorization();
    gateway.Services.AddInletServer(options =>
    {
        options.GeneratedApiAuthorization.Mode =
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints;
        options.GeneratedApiAuthorization.DefaultPolicy = "spring.generated-api";
        options.GeneratedApiAuthorization.AllowAnonymousOptOut = true;
    });
}
else
{
    gateway.AllowAnonymousExplicitly();
    gateway.Services.AddInletServer();
}

gateway.Services.ScanProjectionAssemblies(typeof(BankAccountBalanceProjection).Assembly);

// Add generated domain mapper registrations
gateway.Services.AddSpringDomainServer();

// Add MCP (Model Context Protocol) server with HTTP transport
// Exposes banking domain operations as tools for AI agents via source-generated tool classes.
gateway.Services.AddMcpServer().WithHttpTransport().WithGeneratedMcpTools().WithTools<SpringGatewayPingMcpTools>();
builder.UseMississippi(gateway);
WebApplication app = builder.Build();

// Serve Blazor WebAssembly static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

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