using System.Threading.Tasks;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;


namespace Spring.Server.Infrastructure;

/// <summary>
///     API configuration (controllers, OpenAPI, Scalar) for Spring server.
/// </summary>
internal static class SpringServerApiRegistrations
{
    /// <summary>
    ///     Adds controllers and OpenAPI documentation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSpringApi(
        this IServiceCollection services
    )
    {
        services.AddControllers();
        services.AddOpenApi(ConfigureOpenApi);
        return services;
    }

    private static void ConfigureOpenApi(
        OpenApiOptions options
    )
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
    }
}