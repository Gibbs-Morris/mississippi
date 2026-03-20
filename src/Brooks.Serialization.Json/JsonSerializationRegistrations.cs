using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Serialization.Abstractions;


namespace Mississippi.Brooks.Serialization.Json;

/// <summary>
///     Internal registration helpers for JSON serialization, called by the builder model.
/// </summary>
public static class JsonSerializationRegistrations
{
    /// <summary>
    ///     Registers the JSON serialization provider into the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddJsonSerialization(
        IServiceCollection services
    )
    {
        services.AddSingleton<ISerializationProvider, JsonSerializationProvider>();
    }
}