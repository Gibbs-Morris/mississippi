using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Provides extension methods for registering UX projection components in the dependency injection container.
/// </summary>
public static class UxProjectionRegistrations
{
    /// <summary>
    ///     Adds UX projection infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUxProjections(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IUxProjectionGrainFactory, UxProjectionGrainFactory>();
        return services;
    }
}