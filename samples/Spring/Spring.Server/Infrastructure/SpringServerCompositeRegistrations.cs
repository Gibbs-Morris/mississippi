using Microsoft.AspNetCore.Builder;

using Mississippi.Inlet.Generators.Abstractions;


namespace Spring.Server.Infrastructure;

/// <summary>
///     Composite registrations for Spring server startup.
/// </summary>
[PendingSourceGenerator("Generate Spring server composite registrations and pipeline helpers.")]
internal static class SpringServerCompositeRegistrations
{
    /// <summary>
    ///     Adds all Spring server registrations in a single call.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     This consolidates Spring server setup into one entry point for better developer experience.
    ///     Future source generation could emit this method from an assembly-level attribute.
    /// </remarks>
    public static WebApplicationBuilder AddSpringServer(
        this WebApplicationBuilder builder
    )
    {
        // Observability (OpenTelemetry tracing, metrics, logging)
        builder.AddSpringServerObservability();

        // Orleans client (Aspire-managed clustering)
        builder.AddSpringOrleansClient();

        // API (controllers, OpenAPI, Scalar)
        builder.Services.AddSpringApi();

        // Real-time infrastructure (SignalR, Aqueduct, Inlet, aggregates, projections)
        builder.Services.AddSpringRealtime();
        return builder;
    }

    /// <summary>
    ///     Applies Spring server middleware and endpoint mapping in a single call.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The application for chaining.</returns>
    public static WebApplication UseSpringServer(
        this WebApplication app
    )
    {
        // Middleware (static files, routing)
        app.UseSpringMiddleware();

        // Endpoints (OpenAPI, controllers, Inlet hub, health, SPA fallback)
        app.MapSpringEndpoints();
        return app;
    }
}
