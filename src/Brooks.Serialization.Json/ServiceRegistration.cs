using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Serialization.Abstractions;


namespace Mississippi.EventSourcing.Serialization.Json;

/// <summary>
///     Extension methods for registering JSON serialization services.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    ///     Adds the JSON serialization provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddJsonSerialization(
        this IServiceCollection services
    )
    {
        services.AddSingleton<ISerializationProvider, JsonSerializationProvider>();
        return services;
    }
}