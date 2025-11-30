using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Projections.Projections;


namespace Mississippi.Projections;

/// <summary>
///     Extension methods for registering projection infrastructure components in dependency injection.
/// </summary>
public static class ProjectionsRegistrations
{
    /// <summary>
    ///     Adds the projection infrastructure services to the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same service collection instance for chaining.</returns>
    public static IServiceCollection AddProjections(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IProjectionGrainFactory, ProjectionGrainFactory>();
        return services;
    }
}