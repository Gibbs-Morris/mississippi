using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime;

/// <summary>
///     Provides extension methods for registering snapshot caching components in the dependency injection container.
/// </summary>
public static class SnapshotRegistrations
{
    /// <summary>
    ///     Adds snapshot caching infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapshotCaching(
        this IServiceCollection services
    )
    {
        return AddSnapshotCachingCore(services, static _ => { });
    }

    /// <summary>
    ///     Adds snapshot caching infrastructure with programmatic retention configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The retention options configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapshotCaching(
        this IServiceCollection services,
        Action<SnapshotRetentionOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        return AddSnapshotCachingCore(services, options => options.Configure(configure));
    }

    /// <summary>
    ///     Adds snapshot caching infrastructure with retention options bound from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing snapshot retention options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapshotCaching(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return AddSnapshotCachingCore(services, options => options.Bind(configuration));
    }

    /// <summary>
    ///     Adds snapshot caching infrastructure with an explicit default interval and save-all setting.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultRetainModulus">The positive default interval between persisted snapshots.</param>
    /// <param name="shouldPersistAllSnapshots">Whether every reconstructed snapshot should be persisted.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapshotCaching(
        this IServiceCollection services,
        int defaultRetainModulus,
        bool shouldPersistAllSnapshots = false
    ) =>
        services.AddSnapshotCaching(options =>
        {
            options.DefaultRetainModulus = defaultRetainModulus;
            options.ShouldPersistAllSnapshots = shouldPersistAllSnapshots;
        });

    /// <summary>
    ///     Registers a snapshot state converter for the specified state type.
    /// </summary>
    /// <typeparam name="TSnapshot">The state type to convert.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapshotStateConverter<TSnapshot>(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddTransient<ISnapshotStateConverter<TSnapshot>, SnapshotStateConverter<TSnapshot>>();
        return services;
    }

    private static IServiceCollection AddSnapshotCachingCore(
        IServiceCollection services,
        Action<OptionsBuilder<SnapshotRetentionOptions>> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        OptionsBuilder<SnapshotRetentionOptions> options = services.AddOptions<SnapshotRetentionOptions>();
        configure(options);
        options.ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor
                .Singleton<IValidateOptions<SnapshotRetentionOptions>, SnapshotRetentionOptionsValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, SnapshotRetentionPolicyStartupService>());
        services.TryAddSingleton<ISnapshotGrainFactory, SnapshotGrainFactory>();
        return services;
    }
}