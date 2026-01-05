using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Server;

/// <summary>
///     Extension methods for registering Ripples server services.
/// </summary>
public static class RipplesServerServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Ripples server services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This registers the following services:
    ///         <list type="bullet">
    ///             <item><see cref="InProcessProjectionUpdateNotifier" /> as singleton</item>
    ///             <item><see cref="IProjectionUpdateNotifier" /> pointing to the same instance</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         After calling this, register individual ripple factories using
    ///         <see cref="AddServerRipple{TProjection}" /> for each projection type.
    ///     </para>
    ///     <para>
    ///         The services rely on Orleans grain factory being available for projection access.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddRipplesServer(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the in-process notifier as singleton so all components share notifications
        services.TryAddSingleton<InProcessProjectionUpdateNotifier>();

        // Register the interface pointing to the same instance
        services.TryAddSingleton<IProjectionUpdateNotifier>(sp =>
            sp.GetRequiredService<InProcessProjectionUpdateNotifier>());
        return services;
    }

    /// <summary>
    ///     Adds Ripples server services with a custom notifier.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureNotifier">Configuration action for the notifier.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRipplesServer(
        this IServiceCollection services,
        Action<IServiceCollection> configureNotifier
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureNotifier);

        // Let the caller configure the notifier
        configureNotifier(services);
        return services;
    }

    /// <summary>
    ///     Registers a server ripple factory for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServerRipple<TProjection>(
        this IServiceCollection services
    )
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IRippleFactory<TProjection>, ServerRippleFactory<TProjection>>();
        return services;
    }
}