using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime.Factory;
using Mississippi.Brooks.Runtime.Reader;

using Orleans.Hosting;


namespace Mississippi.Brooks.Runtime;

/// <summary>
///     Extension methods for registering Brooks runtime services in dependency injection.
/// </summary>
public static class BrooksRuntimeRegistrations
{
    /// <summary>
    ///     Configures Orleans silo to support Brooks runtime grains.
    /// </summary>
    /// <param name="builder">The Orleans silo builder.</param>
    /// <param name="configureOptions">
    ///     Optional action to configure <see cref="BrookProviderOptions" />.
    ///     Use this to specify which stream provider name Brooks should use.
    /// </param>
    /// <returns>The modified silo builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method does not configure Orleans streams or storage. The host application
    ///         is responsible for registering that infrastructure separately.
    ///     </para>
    ///     <para>
    ///         Use <paramref name="configureOptions" /> to align
    ///         <c>BrookProviderOptions.OrleansStreamProviderName</c> with the stream provider name
    ///         configured by the host. If no callback is supplied, the default option value remains in effect.
    ///     </para>
    /// </remarks>
    public static ISiloBuilder AddEventSourcing(
        this ISiloBuilder builder,
        Action<BrookProviderOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }

    /// <summary>
    ///     Adds Brooks runtime services to the host application builder.
    ///     This is a convenience method that adds Brooks runtime services to both the service collection
    ///     and configures the Orleans silo.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configureOptions">
    ///     Optional action to configure <see cref="BrookProviderOptions" />.
    ///     Use this to specify which stream provider name Brooks should use.
    /// </param>
    /// <returns>The modified host builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         The host is responsible for configuring Orleans streams before calling this method.
    ///         Use <paramref name="configureOptions" /> to specify the stream provider name that
    ///         Brooks should use for event notifications.
    ///     </para>
    /// </remarks>
    public static HostApplicationBuilder AddEventSourcing(
        this HostApplicationBuilder builder,
        Action<BrookProviderOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Add services to DI container
        builder.Services.AddEventSourcingByService();

        // Configure Orleans silo with options
        builder.UseOrleans(silo => silo.AddEventSourcing(configureOptions));
        return builder;
    }

    /// <summary>
    ///     Adds the core Brooks runtime services to the service collection.
    ///     This registers the Orleans grain factory and reader options needed for Brooks runtime.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The modified service collection for chaining.</returns>
    public static IServiceCollection AddEventSourcingByService(
        this IServiceCollection services
    )
    {
        // Register the grain factory for accessing Orleans grains
        // Register as singleton and expose both public (abstractions) and internal interfaces
        services.AddSingleton<BrookGrainFactory>();
        services.AddSingleton<IBrookGrainFactory>(sp => sp.GetRequiredService<BrookGrainFactory>());
        services.AddSingleton<IInternalBrookGrainFactory>(sp => sp.GetRequiredService<BrookGrainFactory>());

        // Register the stream ID factory for Orleans streams
        services.AddSingleton<IStreamIdFactory, StreamIdFactory>();

        // Register options for brook reader and provider
        services.AddOptions<BrookReaderOptions>();
        services.AddOptions<BrookProviderOptions>();
        return services;
    }
}