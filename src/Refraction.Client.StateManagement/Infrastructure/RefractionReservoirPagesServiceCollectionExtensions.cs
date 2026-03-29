using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Refraction.Client.StateManagement.Infrastructure;

/// <summary>
///     Extension methods for registering Reservoir-explicit Refraction.Client.StateManagement services.
/// </summary>
public static class RefractionReservoirPagesServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Reservoir-explicit Refraction.Client.StateManagement services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     This method registers Reservoir scene-level services. Call this in addition to
    ///     <c>AddRefraction()</c> and <c>AddReservoir()</c> in your application startup.
    /// </remarks>
    public static IServiceCollection AddRefractionReservoirPages(
        this IServiceCollection services
    ) =>
        services;
}