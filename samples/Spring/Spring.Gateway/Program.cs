using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Gateway;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Runtime;
using Mississippi.Inlet.Gateway;
using Mississippi.Inlet.Runtime;

using MississippiSamples.Spring.Domain.Projections.BankAccountBalance;
using MississippiSamples.Spring.Gateway;
using MississippiSamples.Spring.Gateway.Controllers.Aggregates.Mappers;
using MississippiSamples.Spring.Gateway.Controllers.Projections.Mappers;
using MississippiSamples.Spring.Gateway.McpTools;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans.Hosting;
using Orleans.Runtime;

using Scalar.AspNetCore;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
SpringAuthOptions springAuthOptions = builder.Configuration.GetSection("SpringAuth").Get<SpringAuthOptions>() ?? new();
builder.Services.AddOptions<SpringAuthOptions>().Bind(builder.Configuration.GetSection("SpringAuth")).ValidateOnStart();
if (springAuthOptions.Enabled && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Spring local development authentication can only be enabled in Development.");
}

builder.Services.AddOptions<SagaRecoveryOptions>().Bind(builder.Configuration.GetSection("SagaRecovery"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SpringSagaAccessContextProvider>();
builder.Services.AddSingleton<ISagaAccessContextProvider>(sp =>
    sp.GetRequiredService<SpringSagaAccessContextProvider>());
builder.Services.AddSingleton<IValidateOptions<SpringAuthOptions>, SpringSagaAccessOptionsValidator>();
builder.Services.AddHealthChecks().AddCheck<SpringSagaAuthorizationHealthCheck>("spring-saga-access");
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = springAuthOptions.Scheme;
        options.DefaultChallengeScheme = springAuthOptions.Scheme;
    })
    .AddScheme<AuthenticationSchemeOptions, SpringLocalDevAuthenticationHandler>(springAuthOptions.Scheme, _ => { });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("spring.generated-api", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("spring.write", policy => policy.RequireRole("banking-operator"))
    .AddPolicy("spring.transfer", policy => policy.RequireRole("transfer-operator", "banking-operator"))
    .AddPolicy("spring.auth-proof.claim", policy => policy.RequireClaim("spring.permission", "auth-proof"));
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
builder.Services.AddSpringSagaRecoverySupport();

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

// Add Inlet Gateway services for real-time projection updates
if (springAuthOptions.Enabled)
{
    builder.Services.AddInletServer(options =>
    {
        options.GeneratedApiAuthorization.Mode =
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints;
        options.GeneratedApiAuthorization.DefaultPolicy = "spring.generated-api";
        options.GeneratedApiAuthorization.AllowAnonymousOptOut = true;
    });
}
else
{
    builder.Services.AddInletServer();
}

builder.Services.ScanProjectionAssemblies(typeof(BankAccountBalanceProjection).Assembly);

// Add generated domain mapper registrations
builder.Services.AddAuthProofAggregateMappers();
builder.Services.AddBankAccountAggregateMappers();
builder.Services.AddMoneyTransferSagaAggregateMappers();
builder.Services.AddAuthProofProjectionMappers();
builder.Services.AddBankAccountBalanceProjectionMappers();
builder.Services.AddBankAccountLedgerProjectionMappers();
builder.Services.AddFlaggedTransactionsProjectionMappers();
builder.Services.AddMoneyTransferStatusProjectionMappers();

// Add MCP (Model Context Protocol) server with HTTP transport
// Exposes banking domain operations as tools for AI agents via source-generated tool classes.
builder.Services.AddMcpServer().WithHttpTransport().WithGeneratedMcpTools().WithTools<SpringGatewayPingMcpTools>();
WebApplication app = builder.Build();

// Serve Blazor WebAssembly static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (
    context,
    next
) =>
{
    object? previousFingerprint = RequestContext.Get(SpringSagaAccessContextProvider.RequestContextKey);
    try
    {
        string? fingerprint = context.RequestServices.GetRequiredService<SpringSagaAccessContextProvider>()
            .GetFingerprint();
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            RequestContext.Remove(SpringSagaAccessContextProvider.RequestContextKey);
        }
        else
        {
            RequestContext.Set(SpringSagaAccessContextProvider.RequestContextKey, fingerprint);
        }

        await next();
    }
    finally
    {
        if (previousFingerprint is null)
        {
            RequestContext.Remove(SpringSagaAccessContextProvider.RequestContextKey);
        }
        else
        {
            RequestContext.Set(SpringSagaAccessContextProvider.RequestContextKey, previousFingerprint);
        }
    }
});

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
    async (
        HealthCheckService healthCheckService,
        CancellationToken cancellationToken
    ) =>
    {
        HealthReport report = await healthCheckService.CheckHealthAsync(cancellationToken);
        object payload = new
        {
            Status = report.Status == HealthStatus.Unhealthy ? "Unhealthy" : "Healthy",
            Service = "MississippiSamples.Spring.Gateway",
            Checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    Status = entry.Value.Status.ToString(),
                    entry.Value.Description,
                }),
        };
        return report.Status == HealthStatus.Unhealthy
            ? Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable)
            : Results.Ok(payload);
    });

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");
await app.RunAsync();