#pragma warning disable S1133 // Intentional staged deprecation pending issue #237.
using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Serialization.Abstractions;


namespace Mississippi.Brooks.Serialization.Json;

/// <summary>
///     Extension methods for registering JSON serialization services.
/// </summary>
[Obsolete(
    "Legacy runtime composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to RuntimeBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
    false)]
public static class ServiceRegistration
{
    /// <summary>
    ///     Adds the JSON serialization provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete(
        "Legacy runtime composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to RuntimeBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection AddJsonSerialization(
        this IServiceCollection services
    )
    {
        services.AddSingleton<ISerializationProvider, JsonSerializationProvider>();
        return services;
    }
}

#pragma warning restore S1133