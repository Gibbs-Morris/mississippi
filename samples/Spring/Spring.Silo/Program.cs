using Microsoft.AspNetCore.Builder;

using Spring.Silo.Infrastructure;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Observability (OpenTelemetry tracing, metrics, logging)
builder.AddSpringObservability();

// Aspire-managed Azure resources (Table, Blob, Cosmos with Mississippi forwarding)
builder.AddSpringAspireResources();

// Domain (aggregates, projections, application services)
builder.Services.AddSpringDomain();

// Event sourcing infrastructure (Brooks + Snapshots + Cosmos)
builder.Services.AddSpringEventSourcing();

// Orleans silo (Aqueduct + event sourcing)
builder.AddSpringOrleansSilo();
WebApplication app = builder.Build();

// Health check for Aspire orchestration
app.MapSpringHealthCheck();
await app.RunAsync();