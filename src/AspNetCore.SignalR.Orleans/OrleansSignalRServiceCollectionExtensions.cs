// <copyright file="OrleansSignalRServiceCollectionExtensions.cs" company="Gibbs-Morris">
// Proprietary and Confidential.
// All rights reserved.
// </copyright>

using System;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Mississippi.AspNetCore.SignalR.Orleans;

/// <summary>
///     Extension methods for configuring Orleans as a SignalR backplane.
/// </summary>
public static class OrleansSignalRServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Orleans SignalR backplane for the specified hub type.
    /// </summary>
    /// <typeparam name="THub">The type of hub to configure.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers <see cref="OrleansHubLifetimeManager{THub}" /> as the
    ///         <see cref="HubLifetimeManager{THub}" /> implementation for the specified hub.
    ///         The lifetime manager uses Orleans grains for distributed message routing.
    ///     </para>
    ///     <para>
    ///         Prerequisites:
    ///         <list type="bullet">
    ///             <item>Orleans client must be configured and available in DI.</item>
    ///             <item>Stream provider must be configured matching <see cref="OrleansSignalROptions.StreamProviderName" />.</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddOrleansSignalR<THub>(
        this IServiceCollection services
    )
        where THub : Hub
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<HubLifetimeManager<THub>, OrleansHubLifetimeManager<THub>>();
        return services;
    }

    /// <summary>
    ///     Adds the Orleans SignalR backplane for the specified hub type with custom options.
    /// </summary>
    /// <typeparam name="THub">The type of hub to configure.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOptions">An action to configure <see cref="OrleansSignalROptions" />.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers <see cref="OrleansHubLifetimeManager{THub}" /> and configures
    ///         the backplane options. Use this overload to customize stream provider names,
    ///         heartbeat intervals, or stream namespaces.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddOrleansSignalR<THub>(
        this IServiceCollection services,
        Action<OrleansSignalROptions> configureOptions
    )
        where THub : Hub
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.Configure(configureOptions);
        services.TryAddSingleton<HubLifetimeManager<THub>, OrleansHubLifetimeManager<THub>>();
        return services;
    }

    /// <summary>
    ///     Adds the <see cref="ISignalRGrainObserver" /> implementation for sending
    ///     SignalR messages from Orleans grains.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Register this to allow domain grains to inject <see cref="ISignalRGrainObserver" />
    ///         and send SignalR messages to clients without a direct dependency on SignalR.
    ///     </para>
    ///     <para>
    ///         The observer routes messages through the appropriate SignalR client and group
    ///         grains based on the target (connection, group, or all).
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddOrleansSignalRGrainObserver(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ISignalRGrainObserver, OrleansSignalRGrainObserver>();
        return services;
    }
}