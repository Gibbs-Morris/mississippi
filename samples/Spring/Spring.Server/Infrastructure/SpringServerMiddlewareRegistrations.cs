using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using Mississippi.Inlet.Server;

using Scalar.AspNetCore;


namespace Spring.Server.Infrastructure;

/// <summary>
///     Middleware and endpoint configuration for Spring server.
/// </summary>
internal static class SpringServerMiddlewareRegistrations
{
    /// <summary>
    ///     Maps all endpoints (OpenAPI, controllers, Inlet hub, health, SPA fallback).
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The application for chaining.</returns>
    public static WebApplication MapSpringEndpoints(
        this WebApplication app
    )
    {
        MapOpenApiEndpoints(app);
        MapApiEndpoints(app);
        MapHealthEndpoint(app);
        MapSpaFallback(app);
        return app;
    }

    /// <summary>
    ///     Configures middleware pipeline (static files, routing).
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The application for chaining.</returns>
    public static WebApplication UseSpringMiddleware(
        this WebApplication app
    )
    {
        // Serve Blazor WebAssembly static files
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRouting();
        return app;
    }

    private static void MapApiEndpoints(
        WebApplication app
    )
    {
        app.MapControllers();
        app.MapInletHub();
    }

    private static void MapHealthEndpoint(
        WebApplication app
    )
    {
        app.MapGet(
            "/health",
            () => Results.Ok(
                new
                {
                    status = "healthy",
                }));
    }

    private static void MapOpenApiEndpoints(
        WebApplication app
    )
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Spring Bank API");
            options.WithTheme(ScalarTheme.BluePlanet);
            options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    private static void MapSpaFallback(
        WebApplication app
    )
    {
        // Fallback to index.html for SPA routing
        app.MapFallbackToFile("index.html");
    }
}