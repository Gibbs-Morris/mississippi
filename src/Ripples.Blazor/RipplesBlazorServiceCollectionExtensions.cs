using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Blazor;

/// <summary>
///     Extension methods for registering Ripples Blazor services.
/// </summary>
public static class RipplesBlazorServiceCollectionExtensions
{
    /// <summary>
    ///     Registers a ripple for a specific projection type that can be used in Blazor components.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory function to create the ripple.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRipple<TProjection>(
        this IServiceCollection services,
        Func<IServiceProvider, IRipple<TProjection>> factory
    )
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        services.AddScoped(factory);
        return services;
    }

    /// <summary>
    ///     Registers a ripple pool for a specific projection type that can be used in Blazor components.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory function to create the ripple pool.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRipplePool<TProjection>(
        this IServiceCollection services,
        Func<IServiceProvider, IRipplePool<TProjection>> factory
    )
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        services.AddScoped(factory);
        return services;
    }

    /// <summary>
    ///     Adds Ripples Blazor services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="maxCacheSize">
    ///     Maximum number of projections to keep in cache. Default is 50.
    ///     Older unused projections are evicted via LRU when this limit is reached.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers <see cref="IProjectionCache" /> as a scoped service
    ///         (one instance per Blazor circuit). The cache persists projection data across
    ///         page navigations and uses LRU eviction when the cache limit is reached.
    ///     </para>
    ///     <para>
    ///         For Blazor Server, call this followed by:
    ///         <code>
    /// services.AddRipplesBlazor();
    /// services.AddRipplesServer();
    /// services.AddServerRipple&lt;MyProjection&gt;();
    /// </code>
    ///     </para>
    ///     <para>
    ///         For Blazor WebAssembly, call this followed by:
    ///         <code>
    /// services.AddRipplesBlazor();
    /// services.AddRipplesClient(options => options.BaseApiUri = new Uri("https://api.example.com"));
    /// services.AddClientRipple&lt;MyProjection&gt;();
    /// </code>
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddRipplesBlazor(
        this IServiceCollection services,
        int maxCacheSize = 50
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the circuit-scoped projection cache
        services.AddScoped<IProjectionCache>(sp => new ProjectionCache(sp, maxCacheSize));
        return services;
    }
}