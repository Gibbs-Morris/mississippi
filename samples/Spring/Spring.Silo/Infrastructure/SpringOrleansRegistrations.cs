using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

using Mississippi.Aqueduct.Grains;
using Mississippi.EventSourcing.Brooks;

using Orleans.Hosting;


namespace Spring.Silo.Infrastructure;

/// <summary>
///     Orleans silo configuration for Spring.
/// </summary>
internal static class SpringOrleansRegistrations
{
    private const string StreamProviderName = "StreamProvider";

    /// <summary>
    ///     Configures the Orleans silo with Aqueduct and event sourcing.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static WebApplicationBuilder AddSpringOrleansSilo(
        this WebApplicationBuilder builder
    )
    {
        builder.UseOrleans(ConfigureSilo);
        return builder;
    }

    private static void ConfigureSilo(
        ISiloBuilder siloBuilder
    )
    {
        siloBuilder.AddActivityPropagation();

        // Configure Aqueduct for SignalR backplane (uses Aspire-configured stream provider)
        siloBuilder.UseAqueduct(options => options.StreamProviderName = StreamProviderName);

        // Configure event sourcing (must match stream provider name from AppHost)
        siloBuilder.AddEventSourcing(options => options.OrleansStreamProviderName = StreamProviderName);
    }
}