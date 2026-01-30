using Microsoft.AspNetCore.Builder;

using Mississippi.Inlet.Generators.Abstractions;


namespace Spring.Silo.Infrastructure;

/// <summary>
///     Composite registrations for Spring silo startup.
/// </summary>
[PendingSourceGenerator("Generate Spring silo composite registrations and pipeline helpers.")]
internal static class SpringSiloCompositeRegistrations
{
    /// <summary>
    ///     Adds all Spring silo registrations in a single call.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     This consolidates Spring silo setup into one entry point for better developer experience.
    ///     Future source generation could emit this method from an assembly-level attribute.
    /// </remarks>
    public static WebApplicationBuilder AddSpringSilo(
        this WebApplicationBuilder builder
    )
    {
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
        return builder;
    }

    /// <summary>
    ///     Applies Spring silo endpoint mappings in a single call.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The application for chaining.</returns>
    public static WebApplication UseSpringSilo(
        this WebApplication app
    )
    {
        // Health check for Aspire orchestration
        app.MapSpringHealthCheck();
        return app;
    }
}
