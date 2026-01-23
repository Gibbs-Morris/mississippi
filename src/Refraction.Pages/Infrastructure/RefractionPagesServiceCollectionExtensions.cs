using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Refraction.Pages.Infrastructure;

/// <summary>
///     Extension methods for registering Refraction.Pages services.
/// </summary>
public static class RefractionPagesServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Refraction.Pages services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     This method registers scene-level services. Call this in addition to
    ///     <c>AddRefraction()</c> and <c>AddReservoir()</c> in your application startup.
    /// </remarks>
    public static IServiceCollection AddRefractionPages(
        this IServiceCollection services
    ) =>
        // Future: register scene-level services, navigation coordinators, etc.
        services;
}