using System;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Mississippi.Aqueduct.Abstractions;


namespace Mississippi.Aqueduct.Gateway;

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
    ///         The lifetime manager uses Orleans grains for distributed message routing. Connection registries
    ///         and stream managers are keyed by the hub type; custom implementations must use that same key.
    ///         Server identity and heartbeat remain shared by all hubs in this service provider.
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
        services.TryAddSingleton<IServerIdProvider, ServerIdProvider>();
        services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
        services.TryAddSingleton<ILocalMessageSender, LocalMessageSender>();
        services.TryAddSingleton<IHeartbeatManager, HeartbeatManager>();
        services.TryAddSingleton(provider => new HubConnectionRegistries(
            provider.GetKeyedServices<IConnectionRegistry>(KeyedService.AnyKey)));
        services.TryAddKeyedSingleton<IConnectionRegistry, ConnectionRegistry>(typeof(THub));
        services.TryAddKeyedSingleton<IStreamSubscriptionManager, StreamSubscriptionManager>(typeof(THub));
        services.TryAddSingleton<HubLifetimeManager<THub>>(CreateHubLifetimeManager<THub>);
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
    ///         heartbeat intervals, or stream namespaces. Connection registries and stream managers are keyed
    ///         by the hub type; custom implementations must use that key. Server identity and heartbeat remain shared.
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
        return services.AddAqueduct<THub>();
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

    private static AqueductHubLifetimeManager<THub> CreateHubLifetimeManager<THub>(
        IServiceProvider provider
    )
        where THub : Hub =>
        new(
            provider.GetRequiredService<IServerIdProvider>(),
            provider.GetRequiredService<IAqueductGrainFactory>(),
            provider.GetRequiredKeyedService<IConnectionRegistry>(typeof(THub)),
            provider.GetRequiredService<ILocalMessageSender>(),
            provider.GetRequiredService<IHeartbeatManager>(),
            provider.GetRequiredKeyedService<IStreamSubscriptionManager>(typeof(THub)),
            provider.GetRequiredService<ILogger<AqueductHubLifetimeManager<THub>>>(),
            provider.GetRequiredService<HubConnectionRegistries>());
}