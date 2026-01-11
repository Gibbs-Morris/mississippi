using System;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Aqueduct.Abstractions;


namespace Mississippi.Aqueduct;

/// <summary>
///     Extension methods for configuring Aqueduct as a SignalR backplane.
/// </summary>
public static class AqueductRegistrations
{
    /// <summary>
    ///     Adds the Aqueduct backplane for the specified hub type.
    /// </summary>
    /// <typeparam name="THub">The type of hub to configure.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers <see cref="AqueductHubLifetimeManager{THub}" /> as the
    ///         <see cref="HubLifetimeManager{THub}" /> implementation for the specified hub.
    ///         The lifetime manager uses Orleans grains for distributed message routing.
    ///     </para>
    ///     <para>
    ///         Prerequisites:
    ///         <list type="bullet">
    ///             <item>Orleans client must be configured and available in DI.</item>
    ///             <item>Stream provider must be configured matching <see cref="AqueductOptions.StreamProviderName" />.</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddAqueduct<THub>(
        this IServiceCollection services
    )
        where THub : Hub
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
        services.TryAddSingleton<HubLifetimeManager<THub>, AqueductHubLifetimeManager<THub>>();
        return services;
    }

    /// <summary>
    ///     Adds the Aqueduct backplane for the specified hub type with custom options.
    /// </summary>
    /// <typeparam name="THub">The type of hub to configure.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOptions">An action to configure <see cref="AqueductOptions" />.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers <see cref="AqueductHubLifetimeManager{THub}" /> and configures
    ///         the backplane options. Use this overload to customize stream provider names,
    ///         heartbeat intervals, or stream namespaces.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddAqueduct<THub>(
        this IServiceCollection services,
        Action<AqueductOptions> configureOptions
    )
        where THub : Hub
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.Configure(configureOptions);
        services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
        services.TryAddSingleton<HubLifetimeManager<THub>, AqueductHubLifetimeManager<THub>>();
        return services;
    }

    /// <summary>
    ///     Adds the <see cref="IAqueductGrainFactory" /> implementation for resolving
    ///     SignalR grains by strongly-typed keys.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Register this to allow code to obtain SignalR grain references
    ///         without knowing the internal key format. The factory is automatically
    ///         registered by <see cref="AddAqueduct{THub}(IServiceCollection)" />
    ///         and <see cref="AddAqueductNotifier" />.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddAqueductGrainFactory(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
        return services;
    }

    /// <summary>
    ///     Adds the <see cref="IAqueductNotifier" /> implementation for sending
    ///     real-time notifications from Orleans grains.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Register this to allow domain grains to inject <see cref="IAqueductNotifier" />
    ///         and send messages to clients without a direct dependency on SignalR.
    ///     </para>
    ///     <para>
    ///         The notifier routes messages through the appropriate SignalR client and group
    ///         grains based on the target (connection, group, or all).
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddAqueductNotifier(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
        services.TryAddSingleton<IAqueductNotifier, AqueductNotifier>();
        return services;
    }
}