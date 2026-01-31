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
    /// <summary>
    ///     Configures the Orleans silo with Aqueduct and event sourcing.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddSpringOrleansSilo(
        this IHostApplicationBuilder builder
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
        siloBuilder.UseAqueduct();

        // Configure event sourcing (must match stream provider name from AppHost)
        siloBuilder.AddEventSourcing();
    }
}