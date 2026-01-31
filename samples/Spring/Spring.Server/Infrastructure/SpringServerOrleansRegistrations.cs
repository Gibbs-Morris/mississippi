using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

using Orleans.Hosting;


namespace Spring.Server.Infrastructure;

/// <summary>
///     Orleans client configuration for Spring server.
/// </summary>
internal static class SpringServerOrleansRegistrations
{
    /// <summary>
    ///     Adds Aspire-managed Azure Table client and configures Orleans client.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static WebApplicationBuilder AddSpringOrleansClient(
        this WebApplicationBuilder builder
    )
    {
        // Azure Table Storage for Orleans clustering (keyed for Orleans resolution)
        builder.AddKeyedAzureTableServiceClient("clustering");

        // Orleans client (Aspire injects clustering config via environment variables)
        builder.UseOrleansClient(clientBuilder => { clientBuilder.AddActivityPropagation(); });
        return builder;
    }
}