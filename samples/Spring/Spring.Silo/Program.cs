using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans.Hosting;
using Orleans.Runtime;


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

// Add Aspire-managed Azure Storage clients for Orleans clustering and grain state
// These are configured by the AppHost via WithReference
// Must use Keyed variants so Orleans can resolve via GetRequiredKeyedService<T>(serviceKey)
builder.AddKeyedAzureTableServiceClient("clustering");
builder.AddKeyedAzureBlobServiceClient("grainstate");

// Configure Orleans silo - Aspire injects clustering config via environment variables
builder.UseOrleans(siloBuilder => { siloBuilder.AddActivityPropagation(); });
WebApplication app = builder.Build();

// Health check endpoint for Aspire orchestration
app.MapGet(
    "/health",
    (
        ISiloStatusOracle siloStatus
    ) =>
    {
        SiloStatus status = siloStatus.CurrentStatus;
        return status == SiloStatus.Active
            ? Results.Ok(
                new
                {
                    Status = "Healthy",
                    Service = "Spring.Silo",
                    Orleans = status.ToString(),
                })
            : Results.StatusCode(503);
    });
await app.RunAsync();