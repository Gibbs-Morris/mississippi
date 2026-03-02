using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Provides extension methods for registering UX projection components in the dependency injection container.
/// </summary>
[Obsolete("Use RuntimeBuilder.Create() instead. This API will be removed in a future major version.")]
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