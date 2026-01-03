namespace Mississippi.Ripples.Blazor;

using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Abstractions;

/// <summary>
/// Extension methods for registering Ripples Blazor services.
/// </summary>
public static class RipplesBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Adds Ripples Blazor services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the core Blazor infrastructure needed for Ripples.
    /// After calling this, use either <c>AddRipplesServer</c> or <c>AddRipplesClient</c>
    /// depending on the hosting model (Blazor Server vs WebAssembly).
    /// </para>
    /// <para>
    /// For Blazor Server, call this followed by:
    /// <code>
    /// services.AddRipplesBlazor();
    /// services.AddRipplesServer();
    /// services.AddServerRipple&lt;MyProjection&gt;();
    /// </code>
    /// </para>
    /// <para>
    /// For Blazor WebAssembly, call this followed by:
    /// <code>
    /// services.AddRipplesBlazor();
    /// services.AddRipplesClient(options => options.BaseApiUri = new Uri("https://api.example.com"));
    /// services.AddClientRipple&lt;MyProjection&gt;();
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddRipplesBlazor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Currently, the Blazor package provides RippleComponent which doesn't require
        // additional service registrations. This method exists for future expansion
        // and to provide a consistent API entry point.
        return services;
    }

    /// <summary>
    /// Registers a ripple for a specific projection type that can be used in Blazor components.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory function to create the ripple.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRipple<TProjection>(
        this IServiceCollection services,
        Func<IServiceProvider, IRipple<TProjection>> factory)
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.AddScoped(factory);

        return services;
    }

    /// <summary>
    /// Registers a ripple pool for a specific projection type that can be used in Blazor components.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory function to create the ripple pool.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRipplePool<TProjection>(
        this IServiceCollection services,
        Func<IServiceProvider, IRipplePool<TProjection>> factory)
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.AddScoped(factory);

        return services;
    }
}
