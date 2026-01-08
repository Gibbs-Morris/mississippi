using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Extension methods for registering Reservoir Blazor services.
/// </summary>
public static class ReservoirBlazorExtensions
{
    /// <summary>
    ///     Adds the Reservoir store to the service collection for Redux-style state management.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureStore">Optional action to configure the store with feature states.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers <see cref="IStore" /> as a scoped service
    ///         (one instance per Blazor circuit). Use with <see cref="StoreComponent" />
    ///         for automatic state subscription and disposal.
    ///     </para>
    ///     <example>
    ///         <code>
    /// // Register reducers with DI:
    /// services.AddReducer&lt;ToggleSidebarAction, SidebarState, ToggleSidebarReducer&gt;();
    /// 
    /// // Then register the store:
    /// services.AddReservoirBlazor(store =&gt;
    /// {
    ///     store.RegisterState&lt;SidebarState&gt;();
    /// });
    ///         </code>
    ///     </example>
    /// </remarks>
    public static IServiceCollection AddReservoirBlazor(
        this IServiceCollection services,
        Action<Store>? configureStore = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddReservoir(configureStore);
    }
}