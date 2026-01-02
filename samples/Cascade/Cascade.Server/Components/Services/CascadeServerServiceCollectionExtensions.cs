// <copyright file="CascadeServerServiceCollectionExtensions.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Cascade.Server.Components.Services;

/// <summary>
///     Extension methods for registering Cascade.Server services.
/// </summary>
internal static class CascadeServerServiceCollectionExtensions
{
    private static readonly Uri DefaultHubUri = new("/hubs/projections", UriKind.Relative);

    /// <summary>
    ///     Adds the Cascade.Server services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="hubUri">The URI of the SignalR projection hub.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddCascadeServerServices(
        this IServiceCollection services,
        Uri? hubUri = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register UserSession as scoped (one per Blazor circuit)
        services.AddScoped<UserSession>();

        // Register ChatService as scoped (one per Blazor circuit)
        services.AddScoped<IChatService, ChatService>();

        // Register HttpClient for projection API calls using IHttpClientFactory
        services.AddHttpClient("CascadeProjections");

        // Register HubConnection as scoped (one per Blazor circuit)
        services.AddScoped(sp =>
        {
            Uri effectiveUri = hubUri ?? DefaultHubUri;
            return new HubConnectionBuilder().WithUrl(effectiveUri)
                .WithAutomaticReconnect()
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Information); })
                .Build();
        });

        // Register open generic projection subscriber
        services.AddScoped(typeof(IProjectionSubscriber<>), typeof(ProjectionSubscriber<>));

        // Register factory
        services.AddScoped<IProjectionSubscriberFactory, ProjectionSubscriberFactory>();
        return services;
    }
}