using Microsoft.AspNetCore.Builder;

using Spring.Server.Infrastructure;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Observability (OpenTelemetry tracing, metrics, logging)
builder.AddSpringServerObservability();

// Orleans client (Aspire-managed clustering)
builder.AddSpringOrleansClient();

// API (controllers, OpenAPI, Scalar)
builder.Services.AddSpringApi();

// Real-time infrastructure (SignalR, Aqueduct, Inlet, aggregates, projections)
builder.Services.AddSpringRealtime();
WebApplication app = builder.Build();

// Middleware (static files, routing)
app.UseSpringMiddleware();

// Endpoints (OpenAPI, controllers, Inlet hub, health, SPA fallback)
app.MapSpringEndpoints();
await app.RunAsync();