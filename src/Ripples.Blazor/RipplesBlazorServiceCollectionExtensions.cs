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
    ///     Adds the Ripples store to the service collection for Redux-style state management.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureStore">Optional action to configure the store with feature states.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers <see cref="IRippleStore" /> as a scoped service
    ///         (one instance per Blazor circuit). Use with <see cref="RippleComponent" />
    ///         for automatic state subscription and disposal.
    ///     </para>
    ///     <example>
    ///         <code>
    /// // Register reducers with DI:
    /// services.AddReducer&lt;ToggleSidebarAction, SidebarState, ToggleSidebarReducer&gt;();
    ///
    /// // Then register the store:
    /// services.AddRippleStore(store =&gt;
    /// {
    ///     store.RegisterFeatureState&lt;SidebarState&gt;();
    /// });
    /// </code>
    ///     </example>
    /// </remarks>
    public static IServiceCollection AddRippleStore(
        this IServiceCollection services,
        Action<IRippleStore>? configureStore = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IRippleStore>(sp =>
        {
            RippleStore store = new(sp);
            configureStore?.Invoke(store);
            return store;
        });
        return services;
    }
}