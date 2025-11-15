using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;

using Orleans.Hosting;


namespace Mississippi.EventSourcing;

/// <summary>
///     Extension methods for registering EventSourcing services in dependency injection.
/// </summary>
public static class EventSourcingRegistrations
{
    /// <summary>
    ///     Configures Orleans silo to support event sourcing grains.
    ///     This must be called when setting up the Orleans silo.
    /// </summary>
    /// <param name="builder">The Orleans silo builder.</param>
    /// <returns>The modified silo builder for chaining.</returns>
    public static ISiloBuilder AddEventSourcing(
        this ISiloBuilder builder
    )
    {
        // Register memory streams for communication between grains
        builder.AddMemoryStreams("MississippiBrookStreamProvider");
        return builder;
    }

    /// <summary>
    ///     Adds event sourcing services to the host application builder.
    ///     This is a convenience method that adds event sourcing to both the service collection
    ///     and configures the Orleans silo.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The modified host builder for chaining.</returns>
    public static HostApplicationBuilder AddEventSourcing(
        this HostApplicationBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Add services to DI container
        builder.Services.AddEventSourcingByService();

        // Configure Orleans silo
        builder.UseOrleans(silo => silo.AddEventSourcing());
        return builder;
    }

    /// <summary>
    ///     Adds the core event sourcing services to the service collection.
    ///     This registers the Orleans grain factory and reader options needed for event sourcing.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The modified service collection for chaining.</returns>
    public static IServiceCollection AddEventSourcingByService(
        this IServiceCollection services
    )
    {
        // Register the grain factory for accessing Orleans grains
        services.AddSingleton<IBrookGrainFactory, BrookGrainFactory>();

        // Register the stream ID factory for Orleans streams
        services.AddSingleton<IStreamIdFactory, StreamIdFactory>();

        // Register options for brook reader and provider
        services.AddOptions<BrookReaderOptions>();
        services.AddOptions<BrookProviderOptions>();
        return services;
    }
}