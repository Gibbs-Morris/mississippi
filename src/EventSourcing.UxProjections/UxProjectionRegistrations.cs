using System;

using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Common.Abstractions.Builders;
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
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddUxProjections(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
            services.TryAddSingleton<IUxProjectionGrainFactory, UxProjectionGrainFactory>());
        return builder;
    }

    /// <summary>
    ///     Adds UX projection infrastructure services to the service collection.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiServerBuilder AddUxProjections(
        this IMississippiServerBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
            services.TryAddSingleton<IUxProjectionGrainFactory, UxProjectionGrainFactory>());
        return builder;
    }
}