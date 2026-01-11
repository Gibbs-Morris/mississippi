using System;

using Cascade.Components.Services;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.Inlet;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.InProcess;


namespace Cascade.Server.Services;

/// <summary>
///     Extension methods for registering Cascade.Server services.
/// </summary>
internal static class CascadeServerRegistrations
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

        // Register Inlet InProcess infrastructure (IServerProjectionNotifier)
        services.AddInletInProcess();

        // Register InletStore with the subscription effect
        // The effect needs the store to dispatch real-time updates, so we create them together.
        // InletStore.CreateWithEffect takes ownership of the effect and disposes it on store disposal.
        services.AddScoped<IInletStore>(sp =>
        {
            IUxProjectionGrainFactory grainFactory = sp.GetRequiredService<IUxProjectionGrainFactory>();
            IServerProjectionNotifier notifier = sp.GetRequiredService<IServerProjectionNotifier>();
            return InletStore.CreateWithEffect(store =>
                new ServerProjectionSubscriptionEffect(store, grainFactory, notifier));
        });
        return services;
    }
}