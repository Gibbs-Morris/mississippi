using System;

using Cascade.Components.Services;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.Ripples;
using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Server;


namespace Cascade.Server.Services;

/// <summary>
///     Extension methods for registering Cascade.Server services.
/// </summary>
internal static class CascadeServerServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Cascade.Server services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddCascadeServerServices(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register UserSession as scoped (one per Blazor circuit)
        // Also register as IUserContext for shared components
        services.AddScoped<UserSession>();
        services.AddScoped<IUserContext>(sp => sp.GetRequiredService<UserSession>());

        // Register ChatService as scoped (one per Blazor circuit)
        services.AddScoped<IChatService, ChatService>();

        // Register Ripples Server infrastructure (IProjectionUpdateNotifier)
        services.AddRipplesServer();

        // Register RippleStore with the subscription effect
        // The effect needs the store to dispatch real-time updates, so we create them together.
        // RippleStore.CreateWithEffect takes ownership of the effect and disposes it on store disposal.
        services.AddScoped<IRippleStore>(sp =>
        {
            IUxProjectionGrainFactory grainFactory = sp.GetRequiredService<IUxProjectionGrainFactory>();
            IProjectionUpdateNotifier notifier = sp.GetRequiredService<IProjectionUpdateNotifier>();
            return RippleStore.CreateWithEffect(store =>
                new ServerProjectionSubscriptionEffect(store, grainFactory, notifier));
        });
        return services;
    }
}