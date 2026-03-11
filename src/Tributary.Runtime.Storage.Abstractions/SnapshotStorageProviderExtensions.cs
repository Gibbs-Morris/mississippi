#pragma warning disable S1133 // Intentional staged deprecation pending issue #237.
using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Mississippi.Tributary.Runtime.Storage.Abstractions;

/// <summary>
///     Provides helper methods for registering snapshot storage providers and their options.
/// </summary>
[Obsolete(
    "Legacy runtime composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to RuntimeBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
    false)]
public static class SnapshotStorageProviderExtensions
{
    /// <summary>
    ///     Registers a snapshot storage provider that implements <see cref="ISnapshotStorageProvider" />.
    /// </summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <param name="services">The service collection to modify.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete(
        "Legacy runtime composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to RuntimeBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection RegisterSnapshotStorageProvider<TProvider>(
        this IServiceCollection services
    )
        where TProvider : class, ISnapshotStorageProvider
    {
        services.TryAddSingleton<ISnapshotStorageProvider, TProvider>();
        services.AddSingleton<ISnapshotStorageReader>(provider =>
            provider.GetRequiredService<ISnapshotStorageProvider>());
        services.AddSingleton<ISnapshotStorageWriter>(provider =>
            provider.GetRequiredService<ISnapshotStorageProvider>());
        return services;
    }

    /// <summary>
    ///     Registers a snapshot storage provider and binds options via an action.
    /// </summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <typeparam name="TOptions">The options type to bind.</typeparam>
    /// <param name="services">The service collection to modify.</param>
    /// <param name="configureOptions">The options configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete(
        "Legacy runtime composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to RuntimeBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection RegisterSnapshotStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        Action<TOptions> configureOptions
    )
        where TProvider : class, ISnapshotStorageProvider
        where TOptions : class, new()
    {
        services.Configure(configureOptions);
        return services.RegisterSnapshotStorageProvider<TProvider>();
    }

    /// <summary>
    ///     Registers a snapshot storage provider and binds options from configuration.
    /// </summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <typeparam name="TOptions">The options type to bind.</typeparam>
    /// <param name="services">The service collection to modify.</param>
    /// <param name="configuration">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete(
        "Legacy runtime composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to RuntimeBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection RegisterSnapshotStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration
    )
        where TProvider : class, ISnapshotStorageProvider
        where TOptions : class, new()
    {
        services.Configure<TOptions>(configuration);
        return services.RegisterSnapshotStorageProvider<TProvider>();
    }
}

#pragma warning restore S1133