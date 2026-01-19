using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Orleans;
using Orleans.Hosting;

using Spring.Client;
using Spring.Domain;


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
builder.UseOrleansClient(clientBuilder =>
{
    clientBuilder.AddActivityPropagation();
});
WebApplication app = builder.Build();

// Serve Blazor WebAssembly static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

// Map API endpoints
app.MapGet(
    "/api/greet/{name}",
    async (
        string name,
        IGrainFactory grainFactory
    ) =>
    {
        IGreeterGrain grain = grainFactory.GetGrain<IGreeterGrain>(name);
        Spring.Domain.GreetResult grainResult = await grain.GreetAsync();

        // Map Orleans type to Client DTO
        GreetResultDto result = new()
        {
            Greeting = grainResult.Greeting,
            GeneratedAt = grainResult.GeneratedAt,
        };
        return Results.Ok(result);
    });

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");
await app.RunAsync();
