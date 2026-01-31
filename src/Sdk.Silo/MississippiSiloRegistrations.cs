using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Orleans.Hosting;


namespace Mississippi.Sdk.Silo;

/// <summary>
///     Entry points for Mississippi silo registrations.
/// </summary>
public static class MississippiSiloRegistrations
{
    /// <summary>
    ///     Adds Mississippi silo services and returns a fluent builder wrapper.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional action to configure silo options.</param>
    /// <returns>The Mississippi silo builder for chaining.</returns>
    public static MississippiSiloBuilder AddMississippiSilo(
        this IHostApplicationBuilder builder,
        Action<MississippiSiloOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOptions<MississippiSiloOptions>();
        if (configure != null)
        {
            builder.Services.Configure(configure);
        }

        MississippiSiloBuilder mississippiBuilder = new(builder);
        builder.UseOrleans(siloBuilder =>
        {
            foreach (Action<ISiloBuilder> configureSilo in mississippiBuilder.OrleansConfigurations)
            {
                configureSilo(siloBuilder);
            }
        });
        return mississippiBuilder;
    }
}