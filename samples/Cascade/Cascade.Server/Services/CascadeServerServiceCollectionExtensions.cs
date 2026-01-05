using System;

using Cascade.Domain.Projections.ChannelMemberList;
using Cascade.Domain.Projections.ChannelMessages;
using Cascade.Domain.Projections.UserProfile;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Blazor;
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
        services.AddScoped<UserSession>();

        // Register ChatService as scoped (one per Blazor circuit)
        services.AddScoped<IChatService, ChatService>();

        // Register Ripples Blazor infrastructure (IProjectionCache)
        services.AddRipplesBlazor();

        // Register Ripples Server infrastructure (IProjectionUpdateNotifier)
        services.AddRipplesServer();

        // Register server ripples for each projection type used in the UI
        services.AddServerRipple<UserProfileProjection>();
        services.AddServerRipple<ChannelMemberListProjection>();
        services.AddServerRipple<ChannelMessagesProjection>();
        return services;
    }
}