using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using Orleans.Runtime;


namespace Spring.Silo.Infrastructure;

/// <summary>
///     Health check endpoint configuration for Spring silo.
/// </summary>
internal static class SpringHealthRegistrations
{
    /// <summary>
    ///     Maps the health check endpoint for Aspire orchestration.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The application for chaining.</returns>
    public static WebApplication MapSpringHealthCheck(
        this WebApplication app
    )
    {
        app.MapGet("/health", HandleHealthCheck);
        return app;
    }

    private static IResult HandleHealthCheck(
        ISiloStatusOracle siloStatus
    )
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
    }
}