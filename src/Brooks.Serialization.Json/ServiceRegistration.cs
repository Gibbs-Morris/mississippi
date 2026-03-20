using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Serialization.Abstractions;


namespace Mississippi.Brooks.Serialization.Json;

/// <summary>
///     Internal registration helpers for JSON serialization services.
/// </summary>
internal static class ServiceRegistration
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